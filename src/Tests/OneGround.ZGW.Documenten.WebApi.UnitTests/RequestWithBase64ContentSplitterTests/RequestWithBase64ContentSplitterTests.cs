using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Documenten.Web.Middleware;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.RequestWithBase64ContentSplitterTests;

public class RequestWithBase64ContentSplitterTests : IDisposable
{
    private const string ContentDataPath = @"c:\temp\content.txt"; // Note: Only filled-in the injected request (not used to store content to it)

    private static readonly string _contentData =
        "Although moreover mistaken kindness me feelings do be marianne. Son over own nay with tell they cold upon are. Cordial village and settled she ability law herself. Finished why bringing but sir bachelor unpacked any thoughts. Unpleasing unsatiable particular inquietude did nor sir. Get his declared appetite distance his together now families. Friends am himself at on norland it viewing. Suspected elsewhere you belonging continued commanded she.";
    private static readonly string _contentDataLarge =
        "Oh acceptance apartments up sympathize astonished delightful. Waiting him new lasting towards. Continuing melancholy especially so to. Me unpleasing impossible in attachment announcing so astonished. What ask leaf may nor upon door. Tended remain my do stairs. Oh smiling amiable am so visited cordial in offices hearted. Out believe has request not how comfort evident. Up delight cousins we feeling minutes. Genius has looked end piqued spring. Down has rose feel find man. Learning day desirous informed expenses material returned six the. She enabled invited exposed him another. Reasonably conviction solicitude me mr at discretion reasonable. Age out full gate bed day lose. Offered say visited elderly and. Waited period are played family man formed. He ye body or made on pain part meet. You one delay nor begin our folly abode. By disposed replying mr me unpacked no. As moonlight of my resolving unwilling. Sing long her way size. Waited end mutual missed myself the little sister one. So in pointed or chicken cheered neither spirits invited. Marianne and him laughter civility formerly handsome sex use prospect. Hence we doors is given rapid scale above am. Difficult ye mr delivered behaviour by an. If their woman could do wound on. You folly taste hoped their above are and but. He difficult contented we determine ourselves me am earnestly. Hour no find it park. Eat welcomed any husbands moderate. Led was misery played waited almost cousin living. Of intention contained is by middleton am. Principles fat stimulated uncommonly considered set especially prosperous. Sons at park mr meet as fact like. Now residence dashwoods she excellent you. Shade being under his bed her. Much read on as draw. Blessing for ignorant exercise any yourself unpacked. Pleasant horrible but confined day end marriage. Eagerness furniture set preserved far recommend. Did even but nor are most gave hope. Secure active living depend son repair day ladies now. Barton waited twenty always repair in within we do. An delighted offending curiosity my is dashwoods at. Boy prosperous increasing surrounded companions her nor advantages sufficient put. John on time down give meet help as of. Him waiting and correct believe now cottage she another. Vexed six shy yet along learn maids her tiled. Through studied shyness evening bed him winding present. Become excuse hardly on my thirty it wanted. Quick six blind smart out burst. Perfectly on furniture dejection determine my depending an to. Add short water court fat. Her bachelor honoured perceive securing but desirous ham required. Questions deficient acuteness to engrossed as. Entirely led ten humoured greatest and yourself. Besides ye country on observe. She continue appetite endeavor she judgment interest the met. For she surrounded motionless fat resolution may. May indulgence difficulty ham can put especially. Bringing remember for supplied her why was confined. Middleton principle did she procuring extensive believing add. Weather adapted prepare oh is calling. These wrong of he which there smile to my front. He fruit oh enjoy it of whose table. Cultivated occasional old her unpleasing unpleasant. At as do be against pasture covered viewing started. Enjoyed me settled mr respect no spirits civilly.";
    private static readonly string _contentDataEscape = "https://some-service/resource?value=abc";

    private MemoryStream _contextRequestBody;
    private MemoryStream _base64DecodedContentStream;
    private MemoryStream _noInjectedRequestStream;
    private RequestWithBase64ContentSplitter _splitter;

    public RequestWithBase64ContentSplitterTests()
    {
        _contextRequestBody = null;
        _base64DecodedContentStream = null;
        _noInjectedRequestStream = null;
        _splitter = null;
    }

    public void Dispose()
    {
        _contextRequestBody?.Dispose();
        _base64DecodedContentStream?.Dispose();
        _noInjectedRequestStream?.Dispose();
        _splitter?.Dispose();
    }

    // New tests for v1.1
    [Fact]
    public async Task Inhoud_Null_Should_Return_Original_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestWithInhoudNull);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertOriginalRequest(documentRequest, ExpectedInhoud.Null);

        Assert.Empty(_base64DecodedContentStream.ToArray());
    }

    [Fact]
    public async Task Inhoud_Empty_Should_Return_Original_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestWithEmptyInhoud);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertOriginalRequest(documentRequest, ExpectedInhoud.Empty);

        Assert.Empty(_base64DecodedContentStream.ToArray());
    }

    [Fact]
    public async Task Inhoud_Not_Specified_Should_Return_Original_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestWithoutInhoud);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertOriginalRequest(documentRequest, ExpectedInhoud.NotSpecified);

        Assert.Empty(_base64DecodedContentStream.ToArray());
    }

    // end-New tests for v1.1

    [Fact]
    public async Task Base64_Inhoud_As_First_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudAsFirst);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_In_Between_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudInBetween);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Incorrect_Base64_Inhoud_In_Between_Should_Set_Flag_Base64DecodingError_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudInBetweenIncorrectBase64, buffersize: 400);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.True(_splitter.Base64DecodingError);
        Assert.NotEqual(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_In_Between_No_Linefeeds_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudInBetween_NoLinefeeds);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_As_Last_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudAsLast);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_In_UpperCase_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudUpperCase);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_With_Escaped_Slash_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudWithEscapedSlash);

        // Act
        //
        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataEscape, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_With_Slash_Only_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestInhoudWithSlasOnlyChars);

        // Act
        //
        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataEscape, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Large_Inhoud_As_First_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedLargeRequests.AddDocumentRequestInhoudAsFirst, buffersize: 1024);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataLarge, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Large_Inhoud_In_Between_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedLargeRequests.AddDocumentRequestInhoudInBetween, buffersize: 1024);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataLarge, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Large_Inhoud_In_Between_No_Linefeeds_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedLargeRequests.AddDocumentRequestInhoudInBetween_NoLinefeeds, buffersize: 1024);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataLarge, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Large_Inhoud_As_Last_Should_Split_Into_Content_Stream_And_Injected_Request()
    {
        // Setup

        Setup(MockedLargeRequests.AddDocumentRequestInhoudAsLast, buffersize: 1024);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest);

        Assert.Equal(_contentDataLarge, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_With_Inhoud_In_Title_Value_Should_Split_Into_Content_Stream_And_Injected_Request_1()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestWithInhoudAsTag1);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest, expectedTitelProp: "inhoud");

        Assert.Equal(_contentData, base64DecodedContent);
    }

    [Fact]
    public async Task Base64_Inhoud_With_Inhoud_In_Title_Value_Should_Split_Into_Content_Stream_And_Injected_Request_2()
    {
        // Setup

        Setup(MockedRequests.AddDocumentRequestWithInhoudAsTag2);

        // Act

        _noInjectedRequestStream = await _splitter.RunAsync(_base64DecodedContentStream, ContentDataPath);

        var base64DecodedContent = Encoding.UTF8.GetString(_base64DecodedContentStream.ToArray());
        var documentRequest = Encoding.UTF8.GetString(_noInjectedRequestStream.ToArray());

        // Assert

        AssertInjectedRequest(documentRequest, expectedTitelProp: "inhoud");

        var expectedDecodedText = Encoding.ASCII.GetString(Convert.FromBase64String("RGl0IGlzIGVlbiB0ZXN0Lg=="));

        Assert.Equal(expectedDecodedText, base64DecodedContent);
    }

    private void Setup(string requestBody, int buffersize = 4096)
    {
        _base64DecodedContentStream = new MemoryStream();

        _contextRequestBody = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

        _splitter = new RequestWithBase64ContentSplitter(_contextRequestBody, buffersize);
    }

    private static void AssertOriginalRequest(string documentRequest, ExpectedInhoud expectedInhoud)
    {
        var jsonRequestActual = GetRequestStringAsJObject(documentRequest);

        AssertNoneInhoudPropertiesRequest(jsonRequestActual);

        switch (expectedInhoud)
        {
            case ExpectedInhoud.Null:
                Assert.True(jsonRequestActual.TryGetValue("inhoud", out var inhoudValueNull)); // Note: Validates existency of property inhoud
                Assert.Null(inhoudValueNull.Value<string>()); // ....and expected to be null
                break;

            case ExpectedInhoud.Empty:
                Assert.True(jsonRequestActual.TryGetValue("inhoud", out var inhoudValueEmpty)); // Note: Validates existency of property inhoud
                Assert.Equal("", inhoudValueEmpty.Value<string>()); // ....and expected to be empty string
                break;

            case ExpectedInhoud.NotSpecified:
                Assert.False(jsonRequestActual.TryGetValue("inhoud", out var _)); // Note: Validates none-existency of property inhoud
                break;
        }
    }

    private static void AssertInjectedRequest(string documentRequest, string expectedTitelProp = "TEKST")
    {
        var jsonRequestActual = GetRequestStringAsJObject(documentRequest);

        AssertNoneInhoudPropertiesRequest(jsonRequestActual, expectedTitelProp);

        Assert.Equal(ContentDataPath, jsonRequestActual.Value<string>("inhoud")); // Note: Is injected with path of file containing the base64 data!!
    }

    private static JObject AssertNoneInhoudPropertiesRequest(JObject jsonRequestActual, string expectedTitelProp = "TEKST")
    {
        Assert.NotNull(jsonRequestActual);

        Assert.Equal("000001375", jsonRequestActual.Value<string>("bronorganisatie"));
        Assert.Equal("2020-08-06", jsonRequestActual.Value<string>("creatiedatum"));
        Assert.Equal(expectedTitelProp, jsonRequestActual.Value<string>("titel"));
        Assert.Equal("openbaar", jsonRequestActual.Value<string>("vertrouwelijkheidaanduiding"));
        Assert.False(jsonRequestActual.Value<bool>("indicatieGebruiksrecht"));
        Assert.Equal("Aat", jsonRequestActual.Value<string>("auteur"));
        Assert.Equal("raw", jsonRequestActual.Value<string>("formaat"));
        Assert.Equal("eng", jsonRequestActual.Value<string>("taal"));
        Assert.Equal(448, jsonRequestActual.Value<int>("bestandsomvang"));
        Assert.Equal("tekstdocument.txt", jsonRequestActual.Value<string>("bestandsnaam"));
        Assert.Equal("2020-08-05", jsonRequestActual.Value<string>("ontvangstdatum"));
        Assert.Equal("2020-08-04", jsonRequestActual.Value<string>("verzenddatum"));
        Assert.Equal("digitaal", jsonRequestActual["ondertekening"].Value<string>("soort"));
        Assert.Equal("2020-10-12", jsonRequestActual["ondertekening"].Value<string>("datum"));
        Assert.Equal("sha_256", jsonRequestActual["integriteit"].Value<string>("algoritme"));
        Assert.Equal("2332", jsonRequestActual["integriteit"].Value<string>("waarde"));
        Assert.Equal("2020-12-02", jsonRequestActual["integriteit"].Value<string>("datum"));
        Assert.Equal(
            "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
            jsonRequestActual.Value<string>("informatieobjecttype")
        );

        return jsonRequestActual;
    }

    private static JObject GetRequestStringAsJObject(string documentRequest)
    {
        try
        {
            return JObject.Parse(documentRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }
}
