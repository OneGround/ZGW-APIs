using System;
using System.IO;
using System.Security;
using Roxit.ZGW.Common.Helpers;
using Xunit;

namespace Roxit.ZGW.Common.UnitTests;

public class TempFileHelperTests
{
    [Fact]
    public void AssureNotTampered_With_An_Valid_Temp_File_Should_Return()
    {
        // Arrange

        var fullFileName = $"{Path.GetTempPath()}{Guid.NewGuid()}-content.bin";

        // Act
        var ex = Record.Exception(() =>
        {
            TempFileHelper.AssureNotTampered(fullFileName: fullFileName);
        });

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void AssureNotTampered_With_An_Tampered_File_Should_Throw_An_Exception()
    {
        // Arrange
        var exceptionType = typeof(SecurityException);

        // Act
        var ex = Record.Exception(() =>
        {
            TempFileHelper.AssureNotTampered(fullFileName: @"c:\windows\system32\kernel32.dll");
        });

        // Assert
        Assert.NotNull(ex);
        Assert.IsType(exceptionType, ex);
    }

    [Fact]
    public void AssureNotTampered_With_An_Tampered_Path_Should_Throw_An_Exception()
    {
        // Arrange
        var exceptionType = typeof(SecurityException);

        // Note: This is a valid system file:
        var fullFileName = @$"{Path.GetTempPath()}..\..\..\..\..\windows\system32\kernel32.dll";

        // Act
        var ex = Record.Exception(() =>
        {
            TempFileHelper.AssureNotTampered(fullFileName: fullFileName);
        });

        // Assert
        Assert.NotNull(ex);
        Assert.IsType(exceptionType, ex);
    }
}
