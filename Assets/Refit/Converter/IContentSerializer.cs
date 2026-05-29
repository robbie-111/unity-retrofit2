namespace Refit.Converter
{
    /// <summary>
    /// HTTP 바디 직렬화/역직렬화 계약. RefitSettings.ContentSerializer 로 교체 가능하다.
    /// </summary>
    public interface IContentSerializer
    {
        /// <summary>JSON 문자열을 T 타입으로 역직렬화한다.</summary>
        T Deserialize<T>(string body);

        /// <summary>객체를 직렬화하여 전송 가능한 문자열(보통 JSON)로 반환한다.</summary>
        string Serialize(object data);

        /// <summary>POCO 객체를 application/x-www-form-urlencoded 딕셔너리로 변환한다.</summary>
        System.Collections.Generic.Dictionary<string, string> SerializeFormUrlEncoded(object data);
    }
}
