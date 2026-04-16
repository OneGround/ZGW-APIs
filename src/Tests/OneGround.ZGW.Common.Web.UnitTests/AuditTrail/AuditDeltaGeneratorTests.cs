using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.AuditTrail;

public class AuditDeltaGeneratorTests
{
    #region Simple Property Tests

    [Fact]
    public void GenerateDelta_WhenObjectsAreIdentical_ReturnsEmptyDelta()
    {
        // Arrange
        var original = new { Name = "John", Age = 30 };
        var current = new { Name = "John", Age = 30 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    [Fact]
    public void GenerateDelta_WhenSinglePropertyChanged_ReturnsOnlyChangedProperty()
    {
        // Arrange
        var original = new { Name = "John", Age = 30 };
        var current = new { Name = "Jane", Age = 30 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal("Jane", delta["Name"]?.GetValue<string>());
        Assert.False(delta.ContainsKey("Age"));
    }

    [Fact]
    public void GenerateDelta_WhenMultiplePropertiesChanged_ReturnsAllChanges()
    {
        // Arrange
        var original = new
        {
            Name = "John",
            Age = 30,
            City = "NYC",
        };
        var current = new
        {
            Name = "Jane",
            Age = 35,
            City = "NYC",
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(2, delta.Count);
        Assert.Equal("Jane", delta["Name"]?.GetValue<string>());
        Assert.Equal(35, delta["Age"]?.GetValue<int>());
        Assert.False(delta.ContainsKey("City"));
    }

    [Fact]
    public void GenerateDelta_WhenPropertySetToNull_ReturnsNullValue()
    {
        // Arrange
        var original = new { Name = "John", Age = (int?)30 };
        var current = new { Name = "John", Age = (int?)null };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Null(delta["Age"]);
    }

    [Fact]
    public void GenerateDelta_WhenPropertyRemovedFromCurrent_MarksAsRemoved()
    {
        // Arrange
        object original = new { Name = "John", Age = 30 };
        object current = new { Name = "John" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.True(delta.ContainsKey("Age"));
        var removedMarker = delta["Age"]?.AsObject();
        Assert.NotNull(removedMarker);
        Assert.True(removedMarker["__removed"]?.GetValue<bool>());
    }

    [Fact]
    public void GenerateDelta_WhenPropertyAddedToCurrent_ReturnsNewProperty()
    {
        // Arrange
        object original = new { Name = "John" };
        object current = new { Name = "John", Age = 30 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal(30, delta["Age"]?.GetValue<int>());
    }

    #endregion

    #region Nested Object Tests

    [Fact]
    public void GenerateDelta_WhenNestedObjectPropertyChanged_ReturnsNestedDelta()
    {
        // Arrange
        var original = new { Name = "John", Address = new { Street = "123 Main St", City = "NYC" } };
        var current = new { Name = "John", Address = new { Street = "456 Oak Ave", City = "NYC" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.True(delta.ContainsKey("Address"));
        var addressDelta = delta["Address"]?.AsObject();
        Assert.NotNull(addressDelta);
        Assert.Equal("456 Oak Ave", addressDelta["Street"]?.GetValue<string>());
        Assert.False(addressDelta.ContainsKey("City"));
    }

    [Fact]
    public void GenerateDelta_WhenNestedObjectUnchanged_DoesNotIncludeNestedObject()
    {
        // Arrange
        var original = new { Name = "John", Address = new { Street = "123 Main St", City = "NYC" } };
        var current = new { Name = "Jane", Address = new { Street = "123 Main St", City = "NYC" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.True(delta.ContainsKey("Name"));
        Assert.False(delta.ContainsKey("Address"));
    }

    [Fact]
    public void GenerateDelta_WhenDeeplyNestedObjectChanged_ReturnsDeeplyNestedDelta()
    {
        // Arrange
        var original = new { Person = new { Name = "John", Contact = new { Email = "john@test.com", Phone = "555-1234" } } };
        var current = new { Person = new { Name = "John", Contact = new { Email = "jane@test.com", Phone = "555-1234" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        var personDelta = delta["Person"]?.AsObject();
        Assert.NotNull(personDelta);
        var contactDelta = personDelta["Contact"]?.AsObject();
        Assert.NotNull(contactDelta);
        Assert.Equal("jane@test.com", contactDelta["Email"]?.GetValue<string>());
        Assert.False(contactDelta.ContainsKey("Phone"));
    }

    #endregion

    #region Array Tests - Primitive Arrays

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayItemAdded_ReturnsAddedItems()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2, 3 } };
        var current = new { Numbers = new[] { 1, 2, 3, 4 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Single(added);
        Assert.Equal(4, added[0]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayItemRemoved_ReturnsRemovedItems()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2, 3, 4 } };
        var current = new { Numbers = new[] { 1, 2, 3 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Single(removed);
        Assert.Equal(4, removed[0]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayUnchanged_DoesNotIncludeArray()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2, 3 } };
        var current = new { Numbers = new[] { 1, 2, 3 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayHasDuplicates_HandlesCorrectly()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2, 2, 3 } };
        var current = new { Numbers = new[] { 1, 2, 2, 2, 3 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Single(added);
        Assert.Equal(2, added[0]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WhenStringArrayChanged_ReturnsCorrectDelta()
    {
        // Arrange
        var original = new { Tags = new[] { "tag1", "tag2" } };
        var current = new { Tags = new[] { "tag1", "tag3" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Tags"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(added);
        Assert.NotNull(removed);
        Assert.Single(added);
        Assert.Single(removed);
        Assert.Equal("tag3", added[0]?.GetValue<string>());
        Assert.Equal("tag2", removed[0]?.GetValue<string>());
    }

    #endregion

    #region Array Tests - Object Arrays

    [Fact]
    public void GenerateDelta_WhenObjectArrayItemAddedWithId_ReturnsAddedItems()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" }, new { Id = 2, Name = "Item2" } } };
        var current = new { Items = new[] { new { Id = 1, Name = "Item1" }, new { Id = 2, Name = "Item2" }, new { Id = 3, Name = "Item3" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Single(added);
        Assert.Equal(3, added[0]?["Id"]?.GetValue<int>());
        Assert.Equal("Item3", added[0]?["Name"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayItemRemovedWithId_ReturnsRemovedItems()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" }, new { Id = 2, Name = "Item2" } } };
        var current = new { Items = new[] { new { Id = 1, Name = "Item1" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Single(removed);
        Assert.Equal(2, removed[0]?["Id"]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayItemUpdatedWithId_ReturnsUpdatedItems()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" }, new { Id = 2, Name = "Item2" } } };
        var current = new { Items = new[] { new { Id = 1, Name = "Item1 Updated" }, new { Id = 2, Name = "Item2" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.NotNull(updated);
        Assert.Single(updated);
        Assert.Equal(1, updated[0]?["Id"]?.GetValue<int>());
        Assert.Equal("Item1 Updated", updated[0]?["Name"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayUnchanged_DoesNotIncludeArray()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" } } };
        var current = new { Items = new[] { new { Id = 1, Name = "Item1" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void GenerateDelta_ComplexObjectWithNestedArraysAndObjects_ReturnsCorrectDelta()
    {
        // Arrange
        var original = new
        {
            Name = "John",
            Age = 30,
            Address = new { Street = "123 Main St", City = "NYC" },
            Hobbies = new[] { "Reading", "Gaming" },
        };
        var current = new
        {
            Name = "John",
            Age = 31,
            Address = new { Street = "456 Oak Ave", City = "NYC" },
            Hobbies = new[] { "Reading", "Cooking" },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(3, delta.Count);
        Assert.Equal(31, delta["Age"]?.GetValue<int>());

        var addressDelta = delta["Address"]?.AsObject();
        Assert.NotNull(addressDelta);
        Assert.Equal("456 Oak Ave", addressDelta["Street"]?.GetValue<string>());

        var hobbiesDelta = delta["Hobbies"]?.AsObject();
        Assert.NotNull(hobbiesDelta);
        Assert.NotNull(hobbiesDelta["added"]);
        Assert.NotNull(hobbiesDelta["removed"]);
    }

    [Fact]
    public void GenerateDelta_WhenEntireNestedObjectChanged_ReturnsAllNestedChanges()
    {
        // Arrange
        var original = new
        {
            Person = new
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 30,
            },
        };
        var current = new
        {
            Person = new
            {
                FirstName = "Jane",
                LastName = "Smith",
                Age = 25,
            },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var personDelta = delta["Person"]?.AsObject();
        Assert.NotNull(personDelta);
        Assert.Equal(3, personDelta.Count);
        Assert.Equal("Jane", personDelta["FirstName"]?.GetValue<string>());
        Assert.Equal("Smith", personDelta["LastName"]?.GetValue<string>());
        Assert.Equal(25, personDelta["Age"]?.GetValue<int>());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateDelta_WithEmptyObjects_ReturnsEmptyDelta()
    {
        // Arrange
        var original = new { };
        var current = new { };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    [Fact]
    public void GenerateDelta_WithEmptyArrays_DoesNotIncludeArray()
    {
        // Arrange
        var original = new { Items = new int[] { } };
        var current = new { Items = new int[] { } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    [Fact]
    public void GenerateDelta_WhenArrayBecomesEmpty_ReturnsRemovedItems()
    {
        // Arrange
        var original = new { Items = new[] { 1, 2, 3 } };
        var current = new { Items = new int[] { } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Equal(3, removed.Count);
    }

    [Fact]
    public void GenerateDelta_WithBooleanProperties_TracksChangesCorrectly()
    {
        // Arrange
        var original = new { IsActive = true, IsVerified = false };
        var current = new { IsActive = false, IsVerified = false };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.False(delta["IsActive"]?.GetValue<bool>());
    }

    [Fact]
    public void GenerateDelta_WithNumericTypes_TracksChangesCorrectly()
    {
        // Arrange
        var original = new
        {
            IntValue = 10,
            DoubleValue = 10.5,
            DecimalValue = 10.5m,
        };
        var current = new
        {
            IntValue = 20,
            DoubleValue = 20.5,
            DecimalValue = 10.5m,
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(2, delta.Count);
        Assert.Equal(20, delta["IntValue"]?.GetValue<int>());
        Assert.Equal(20.5, delta["DoubleValue"]?.GetValue<double>());
        Assert.False(delta.ContainsKey("DecimalValue"));
    }

    #endregion

    #region Null Handling Tests

    [Fact]
    public void GenerateDelta_WhenPropertyChangedFromNullToValue_ReturnsNewValue()
    {
        // Arrange
        var original = new { Name = (string?)null, Age = 30 };
        var current = new { Name = "John", Age = 30 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal("John", delta["Name"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WhenNullableNumberChangedToNull_ReturnsNull()
    {
        // Arrange
        var original = new { Value = (int?)42 };
        var current = new { Value = (int?)null };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Null(delta["Value"]);
    }

    [Fact]
    public void GenerateDelta_WhenNestedObjectChangedToNull_ReturnsNull()
    {
        // Arrange
        object original = new { Address = new { Street = "Main St" } };
        object current = new { Address = (object?)null };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Null(delta["Address"]);
    }

    [Fact]
    public void GenerateDelta_WhenArrayChangedToNull_ReturnsNull()
    {
        // Arrange
        var original = new { Items = new[] { 1, 2, 3 } };
        var current = new { Items = (int[]?)null };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Null(delta["Items"]);
    }

    [Fact]
    public void GenerateDelta_WhenBothObjectsHaveNullProperty_DoesNotIncludeProperty()
    {
        // Arrange
        var original = new { Name = (string?)null };
        var current = new { Name = (string?)null };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    #endregion

    #region DateTime and Special Type Tests

    [Fact]
    public void GenerateDelta_WhenDateTimeChanged_ReturnsNewDateTime()
    {
        // Arrange
        var original = new { CreatedAt = new DateTime(2024, 1, 1) };
        var current = new { CreatedAt = new DateTime(2024, 1, 2) };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["CreatedAt"]);
    }

    [Fact]
    public void GenerateDelta_WhenGuidChanged_ReturnsNewGuid()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var original = new { Id = guid1 };
        var current = new { Id = guid2 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Id"]);
    }

    [Fact]
    public void GenerateDelta_WhenDateTimeUnchanged_DoesNotIncludeDateTime()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);
        var original = new { CreatedAt = date };
        var current = new { CreatedAt = date };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Empty(delta);
    }

    [Fact]
    public void GenerateDelta_WithEnumValues_TracksChangesCorrectly()
    {
        // Arrange
        var original = new { Status = DayOfWeek.Monday };
        var current = new { Status = DayOfWeek.Tuesday };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Status"]);
    }

    #endregion

    #region Object Array Without Id Tests

    [Fact]
    public void GenerateDelta_WhenObjectArrayWithoutId_UsesHashComparison()
    {
        // Arrange
        var original = new { Items = new[] { new { Name = "Item1", Value = 100 }, new { Name = "Item2", Value = 200 } } };
        var current = new { Items = new[] { new { Name = "Item1", Value = 100 }, new { Name = "Item3", Value = 300 } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(added);
        Assert.NotNull(removed);
        Assert.Single(added);
        Assert.Single(removed);
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayHasMixedIdAndNonId_HandlesCorrectly()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" } } };
        var current = new { Items = new[] { new { Id = 1, Name = "Item1 Updated" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.NotNull(updated);
        Assert.Single(updated);
    }

    #endregion

    #region Nested Array Tests

    [Fact]
    public void GenerateDelta_WhenNestedArrayChanged_ReturnsNestedArrayDelta()
    {
        // Arrange
        var original = new { Matrix = new { Rows = new[] { 1, 2, 3 } } };
        var current = new { Matrix = new { Rows = new[] { 1, 2, 4 } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var matrixDelta = delta["Matrix"]?.AsObject();
        Assert.NotNull(matrixDelta);
        var rowsDelta = matrixDelta["Rows"]?.AsObject();
        Assert.NotNull(rowsDelta);
    }

    [Fact]
    public void GenerateDelta_WithArrayOfNestedObjects_TracksChangesInNestedObjects()
    {
        // Arrange
        var original = new { Users = new[] { new { Id = 1, Profile = new { Name = "John", Email = "john@test.com" } } } };
        var current = new { Users = new[] { new { Id = 1, Profile = new { Name = "John", Email = "john@example.com" } } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Users"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.NotNull(updated);
        Assert.Single(updated);
    }

    [Fact]
    public void GenerateDelta_WithArrayOfArrays_HandlesCorrectly()
    {
        // Arrange
        var original = new { Id = 1, Tags = new[] { "tag1", "tag2" } };
        var current = new { Id = 1, Tags = new[] { "tag1", "tag3" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var tagsDelta = delta["Tags"]?.AsObject();
        Assert.NotNull(tagsDelta);
        Assert.NotNull(tagsDelta["added"]);
        Assert.NotNull(tagsDelta["removed"]);
    }

    #endregion

    #region Multiple Simultaneous Changes Tests

    [Fact]
    public void GenerateDelta_WithMultipleArrayOperations_ReturnsAllOperations()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Name = "Item1" }, new { Id = 2, Name = "Item2" }, new { Id = 3, Name = "Item3" } } };
        var current = new
        {
            Items = new[] { new { Id = 1, Name = "Item1 Updated" }, new { Id = 3, Name = "Item3" }, new { Id = 4, Name = "Item4" } },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        Assert.NotNull(arrayDelta["added"]);
        Assert.NotNull(arrayDelta["removed"]);
        Assert.NotNull(arrayDelta["updated"]);

        var added = arrayDelta["added"]?.AsArray();
        var removed = arrayDelta["removed"]?.AsArray();
        var updated = arrayDelta["updated"]?.AsArray();

        Assert.Single(added!);
        Assert.Single(removed!);
        Assert.Single(updated!);
    }

    [Fact]
    public void GenerateDelta_WithChangesAtAllLevels_ReturnsCompleteHierarchy()
    {
        // Arrange
        var original = new { TopLevel = "Value1", Middle = new { MiddleLevel = "Value2", Deep = new { DeepLevel = "Value3" } } };
        var current = new { TopLevel = "NewValue1", Middle = new { MiddleLevel = "NewValue2", Deep = new { DeepLevel = "NewValue3" } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(2, delta.Count);
        Assert.Equal("NewValue1", delta["TopLevel"]?.GetValue<string>());

        var middleDelta = delta["Middle"]?.AsObject();
        Assert.NotNull(middleDelta);
        Assert.Equal("NewValue2", middleDelta["MiddleLevel"]?.GetValue<string>());

        var deepDelta = middleDelta["Deep"]?.AsObject();
        Assert.NotNull(deepDelta);
        Assert.Equal("NewValue3", deepDelta["DeepLevel"]?.GetValue<string>());
    }

    #endregion

    #region Property Type Changes Tests

    [Fact]
    public void GenerateDelta_WhenPropertyTypeChangesFromStringToNumber_ReturnsNewValue()
    {
        // Arrange
        object original = new { Value = "123" };
        object current = new { Value = 123 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Value"]);
    }

    [Fact]
    public void GenerateDelta_WhenPropertyTypeChangesFromObjectToArray_ReturnsNewValue()
    {
        // Arrange
        object original = new { Data = new { Key = "Value" } };
        object current = new { Data = new[] { 1, 2, 3 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Data"]);
    }

    #endregion

    #region Empty and Single Element Tests

    [Fact]
    public void GenerateDelta_WhenArrayGoesFromEmptyToPopulated_ReturnsAddedItems()
    {
        // Arrange
        var original = new { Items = new int[] { } };
        var current = new { Items = new[] { 1, 2, 3 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Equal(3, added.Count);
    }

    [Fact]
    public void GenerateDelta_WithSingleElementArray_TracksCorrectly()
    {
        // Arrange
        var original = new { Items = new[] { 1 } };
        var current = new { Items = new[] { 2 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(added);
        Assert.NotNull(removed);
        Assert.Single(added);
        Assert.Single(removed);
    }

    [Fact]
    public void GenerateDelta_WithSingleProperty_TracksCorrectly()
    {
        // Arrange
        var original = new { Value = 1 };
        var current = new { Value = 2 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal(2, delta["Value"]?.GetValue<int>());
    }

    #endregion

    #region Duplicate Values in Arrays Tests

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayHasMultipleDuplicatesAdded_ReturnsCorrectCount()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2 } };
        var current = new { Numbers = new[] { 1, 2, 2, 2 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Equal(2, added.Count);
    }

    [Fact]
    public void GenerateDelta_WhenPrimitiveArrayHasMultipleDuplicatesRemoved_ReturnsCorrectCount()
    {
        // Arrange
        var original = new { Numbers = new[] { 1, 2, 2, 2 } };
        var current = new { Numbers = new[] { 1, 2 } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Equal(2, removed.Count);
    }

    [Fact]
    public void GenerateDelta_WhenStringArrayHasRepeatingPatterns_HandlesCorrectly()
    {
        // Arrange
        var original = new { Tags = new[] { "A", "B", "A", "B" } };
        var current = new { Tags = new[] { "A", "B", "C" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Tags"]?.AsObject();
        Assert.NotNull(arrayDelta);
    }

    #endregion

    #region Large Object Tests

    [Fact]
    public void GenerateDelta_WithManyProperties_TracksOnlyChanged()
    {
        // Arrange
        var original = new
        {
            Prop1 = "Value1",
            Prop2 = "Value2",
            Prop3 = "Value3",
            Prop4 = "Value4",
            Prop5 = "Value5",
            Prop6 = "Value6",
            Prop7 = "Value7",
            Prop8 = "Value8",
            Prop9 = "Value9",
            Prop10 = "Value10",
        };
        var current = new
        {
            Prop1 = "Value1",
            Prop2 = "NewValue2",
            Prop3 = "Value3",
            Prop4 = "Value4",
            Prop5 = "NewValue5",
            Prop6 = "Value6",
            Prop7 = "Value7",
            Prop8 = "Value8",
            Prop9 = "Value9",
            Prop10 = "Value10",
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(2, delta.Count);
        Assert.Equal("NewValue2", delta["Prop2"]?.GetValue<string>());
        Assert.Equal("NewValue5", delta["Prop5"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WithLargeArray_TracksChangesEfficiently()
    {
        // Arrange
        var originalArray = Enumerable.Range(1, 100).ToArray();
        var currentArray = Enumerable.Range(2, 100).ToArray();
        var original = new { Numbers = originalArray };
        var current = new { Numbers = currentArray };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Numbers"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var added = arrayDelta["added"]?.AsArray();
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(added);
        Assert.NotNull(removed);
    }

    #endregion

    #region String Edge Cases Tests

    [Fact]
    public void GenerateDelta_WithEmptyString_TracksCorrectly()
    {
        // Arrange
        var original = new { Name = "John" };
        var current = new { Name = "" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal("", delta["Name"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WithWhitespaceString_TracksCorrectly()
    {
        // Arrange
        var original = new { Name = "John" };
        var current = new { Name = "   " };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal("   ", delta["Name"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateDelta_WithSpecialCharacters_TracksCorrectly()
    {
        // Arrange
        var original = new { Text = "Hello" };
        var current = new { Text = "Hello\nWorld\t!" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Text"]);
    }

    [Fact]
    public void GenerateDelta_WithUnicodeCharacters_TracksCorrectly()
    {
        // Arrange
        var original = new { Text = "Hello" };
        var current = new { Text = "Hello 世界 🌍" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Text"]);
    }

    #endregion

    #region Numeric Edge Cases Tests

    [Fact]
    public void GenerateDelta_WithZeroValues_TracksCorrectly()
    {
        // Arrange
        var original = new { Value = 10 };
        var current = new { Value = 0 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal(0, delta["Value"]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WithNegativeNumbers_TracksCorrectly()
    {
        // Arrange
        var original = new { Value = 10 };
        var current = new { Value = -10 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.Equal(-10, delta["Value"]?.GetValue<int>());
    }

    [Fact]
    public void GenerateDelta_WithFloatingPointPrecision_TracksCorrectly()
    {
        // Arrange
        var original = new { Value = 1.1 };
        var current = new { Value = 1.1000000001 };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert - These should be different due to JSON serialization precision
        Assert.NotEmpty(delta);
    }

    [Fact]
    public void GenerateDelta_WithVeryLargeNumbers_TracksCorrectly()
    {
        // Arrange
        var original = new { Value = 9999999999L };
        var current = new { Value = 9999999998L };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        Assert.NotNull(delta["Value"]);
    }

    #endregion

    #region Multiple Property Removal Tests

    [Fact]
    public void GenerateDelta_WhenMultiplePropertiesRemoved_MarksAllAsRemoved()
    {
        // Arrange
        object original = new
        {
            Name = "John",
            Age = 30,
            City = "NYC",
        };
        object current = new { Name = "John" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(2, delta.Count);
        Assert.True(delta["Age"]?.AsObject()?["__removed"]?.GetValue<bool>());
        Assert.True(delta["City"]?.AsObject()?["__removed"]?.GetValue<bool>());
    }

    [Fact]
    public void GenerateDelta_WhenNestedPropertyRemoved_MarksAsRemoved()
    {
        // Arrange
        object original = new { Person = new { Name = "John", Age = 30 } };
        object current = new { Person = new { Name = "John" } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var personDelta = delta["Person"]?.AsObject();
        Assert.NotNull(personDelta);
        Assert.True(personDelta["Age"]?.AsObject()?["__removed"]?.GetValue<bool>());
    }

    #endregion

    #region Complex Object Array Scenarios

    [Fact]
    public void GenerateDelta_WhenObjectArrayHasMultipleUpdates_TracksAllUpdates()
    {
        // Arrange
        var original = new
        {
            Items = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Item1",
                    Value = 100,
                },
                new
                {
                    Id = 2,
                    Name = "Item2",
                    Value = 200,
                },
                new
                {
                    Id = 3,
                    Name = "Item3",
                    Value = 300,
                },
            },
        };
        var current = new
        {
            Items = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Item1 Updated",
                    Value = 150,
                },
                new
                {
                    Id = 2,
                    Name = "Item2 Updated",
                    Value = 250,
                },
                new
                {
                    Id = 3,
                    Name = "Item3",
                    Value = 300,
                },
            },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Count);
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayHasMultipleUpdates_TracksAsDeletedAndAdded()
    {
        // Arrange
        var original = new
        {
            Items = new[] { new { Name = "Item1", Value = 100 }, new { Name = "Item2", Value = 200 }, new { Name = "Item3", Value = 300 } },
        };
        var current = new
        {
            Items = new[]
            {
                new { Name = "Item1 Updated", Value = 150 },
                new { Name = "Item2 Updated", Value = 250 },
                new { Name = "Item3", Value = 300 },
            },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.Null(updated);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Equal(2, added.Count);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Equal(2, removed.Count);
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayHasNestedChanges_TracksNestedChanges()
    {
        // Arrange
        var original = new { Items = new[] { new { Id = 1, Details = new { Name = "Detail1", Count = 5 } } } };
        var current = new { Items = new[] { new { Id = 1, Details = new { Name = "Detail1 Updated", Count = 5 } } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.NotNull(updated);
        Assert.Single(updated);
    }

    [Fact]
    public void GenerateDelta_WhenObjectArrayHasNestedChanges_TracksNestedChanges_2()
    {
        // Arrange
        var original = new { Items = new[] { new { Identification = "1ay", Details = new { Name = "Detail1", Count = 5 } } } };
        var current = new { Items = new[] { new { Identification = "5ta", Details = new { Name = "Detail1 Updated", Count = 5 } } } };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        var arrayDelta = delta["Items"]?.AsObject();
        Assert.NotNull(arrayDelta);
        var updated = arrayDelta["updated"]?.AsArray();
        Assert.Null(updated);
        var added = arrayDelta["added"]?.AsArray();
        Assert.NotNull(added);
        Assert.Single(added);
        var removed = arrayDelta["removed"]?.AsArray();
        Assert.NotNull(removed);
        Assert.Single(removed);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void GenerateDelta_UserProfileUpdateScenario_TracksCorrectly()
    {
        // Arrange
        var original = new
        {
            UserId = 123,
            Profile = new
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "555-1234",
                Address = new
                {
                    Street = "123 Main St",
                    City = "NYC",
                    ZipCode = "10001",
                },
            },
            Preferences = new { Theme = "dark", Notifications = true },
        };
        var current = new
        {
            UserId = 123,
            Profile = new
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@example.com",
                Phone = "555-1234",
                Address = new
                {
                    Street = "456 Oak Ave",
                    City = "NYC",
                    ZipCode = "10001",
                },
            },
            Preferences = new { Theme = "dark", Notifications = true },
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Single(delta);
        var profileDelta = delta["Profile"]?.AsObject();
        Assert.NotNull(profileDelta);
        Assert.Equal(3, profileDelta.Count); // LastName, Email, and Address changed
        Assert.Equal("Smith", profileDelta["LastName"]?.GetValue<string>());
        Assert.Equal("john.smith@example.com", profileDelta["Email"]?.GetValue<string>());
        Assert.NotNull(profileDelta["Address"]);
    }

    [Fact]
    public void GenerateDelta_OrderWithItemsScenario_TracksCorrectly()
    {
        // Arrange
        var original = new
        {
            OrderId = 1001,
            Status = "Pending",
            Items = new[]
            {
                new
                {
                    Item = 1,
                    Product = "Widget",
                    Quantity = 2,
                    Price = 10.00,
                },
                new
                {
                    Item = 2,
                    Product = "Gadget",
                    Quantity = 1,
                    Price = 20.00,
                },
            },
            Total = 40.00,
        };
        var current = new
        {
            OrderId = 1001,
            Status = "Shipped",
            Items = new[]
            {
                new
                {
                    Item = 1,
                    Product = "Widget",
                    Quantity = 3,
                    Price = 10.00,
                },
                new
                {
                    Item = 2,
                    Product = "Gadget",
                    Quantity = 1,
                    Price = 20.00,
                },
            },
            Total = 50.00,
        };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta(original, current);

        // Assert
        Assert.Equal(3, delta.Count);
        Assert.Equal("Shipped", delta["Status"]?.GetValue<string>());
        Assert.Equal(50.00, delta["Total"]?.GetValue<double>());
        var itemsDelta = delta["Items"]?.AsObject();
        Assert.NotNull(itemsDelta);
    }

    #endregion

    #region PropertiesUsingCurrentValue Tests (with Complex type geometrie)

    [Fact]
    public void GenerateDelta_WithPropertiesUsingCurrentValue_UsesReplaceMarker()
    {
        // Arrange - Simulating zaakgeometrie change from GeometryCollection to Point
        var original = new
        {
            toelichting = "Testzaak",
            omschrijving = "Zaak with geometrie type 'GeometryCollection'",
            zaakgeometrie = new
            {
                type = "GeometryCollection",
                geometries = new[]
                {
                    new
                    {
                        type = "Polygon",
                        coordinates = new[]
                        {
                            new[] { new[] { 196600.675, 543516.651 }, new[] { 196601.447, 543516.117 }, new[] { 196600.675, 543516.651 } },
                        },
                    },
                    new
                    {
                        type = "Polygon",
                        coordinates = new[]
                        {
                            new[] { new[] { 999999.033, 543517.076 }, new[] { 888888.033, 543517.074 }, new[] { 777777.033, 543517.076 } },
                        },
                    },
                },
            },
            bronorganisatie = "805307631",
        };

        var current = new
        {
            toelichting = "Testzaak",
            omschrijving = "Zaak with modified geometrie type 'Point'",
            zaakgeometrie = new { type = "Point", coordinates = new[] { 68436.707, 421115.413 } },
            bronorganisatie = "805307631",
        };

        var propertiesUsingCurrentValue = new List<string> { "zaakgeometrie" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta<object>(original, current, propertiesUsingCurrentValue);

        // Assert
        Assert.Equal(2, delta.Count); // omschrijving and zaakgeometrie
        Assert.Equal("Zaak with modified geometrie type 'Point'", delta["omschrijving"]?.GetValue<string>());

        // Verify zaakgeometrie has __replace marker
        var zaakgeometrieDelta = delta["zaakgeometrie"]?.AsObject();
        Assert.NotNull(zaakgeometrieDelta);
        Assert.True(zaakgeometrieDelta.ContainsKey("__replace"));

        // Verify the replacement value contains the new structure
        var replacementValue = zaakgeometrieDelta["__replace"]?.AsObject();
        Assert.NotNull(replacementValue);
        Assert.Equal("Point", replacementValue["type"]?.GetValue<string>());

        var coordinates = replacementValue["coordinates"]?.AsArray();
        Assert.NotNull(coordinates);
        Assert.Equal(2, coordinates.Count);
        Assert.Equal(68436.707, coordinates[0]?.GetValue<double>());
        Assert.Equal(421115.413, coordinates[1]?.GetValue<double>());

        // Verify old 'geometries' property is NOT in the replacement value
        Assert.False(replacementValue.ContainsKey("geometries"));
    }

    [Fact]
    public void GenerateDelta_WithPropertiesUsingCurrentValue_HandlesNull()
    {
        // Arrange
        var original = new { name = "Test", geometry = new { type = "Point", coordinates = new[] { 1.0, 2.0 } } };

        var current = new { name = "Test", geometry = (object?)null };

        var propertiesUsingCurrentValue = new List<string> { "geometry" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta<object>(original, current, propertiesUsingCurrentValue);

        // Assert
        Assert.Single(delta);
        Assert.True(delta.ContainsKey("geometry"));
        Assert.Null(delta["geometry"]);
    }

    [Fact]
    public void GenerateDelta_WithPropertiesUsingCurrentValue_OnlyAffectsSpecifiedProperties()
    {
        // Arrange
        var original = new { normalProp = new { nested = "old" }, replaceProp = new { nested = "old" } };

        var current = new { normalProp = new { nested = "new" }, replaceProp = new { nested = "new", extra = "added" } };

        var propertiesUsingCurrentValue = new List<string> { "replaceProp" };

        // Act
        var delta = AuditDeltaGenerator.GenerateDelta<object>(original, current, propertiesUsingCurrentValue);

        // Assert
        Assert.Equal(2, delta.Count);

        // normalProp should use standard delta (only nested property)
        var normalDelta = delta["normalProp"]?.AsObject();
        Assert.NotNull(normalDelta);
        Assert.False(normalDelta.ContainsKey("__replace"));
        Assert.Equal("new", normalDelta["nested"]?.GetValue<string>());

        // replaceProp should use __replace marker (entire object)
        var replaceDelta = delta["replaceProp"]?.AsObject();
        Assert.NotNull(replaceDelta);
        Assert.True(replaceDelta.ContainsKey("__replace"));

        var replaceValue = replaceDelta["__replace"]?.AsObject();
        Assert.NotNull(replaceValue);
        Assert.Equal("new", replaceValue["nested"]?.GetValue<string>());
        Assert.Equal("added", replaceValue["extra"]?.GetValue<string>());
    }

    #endregion
}
