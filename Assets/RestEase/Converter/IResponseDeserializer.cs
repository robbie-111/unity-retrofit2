namespace RestEase.Converter
{
    /// <summary>
    /// HTTP 응답 바디의 역직렬화 계약.
    /// </summary>
    public interface IResponseDeserializer
    {
        /// <summary>JSON 문자열을 T 타입 객체로 역직렬화한다.</summary>
        T Deserialize<T>(string body);

        /// <summary>객체를 JSON 문자열로 직렬화한다.</summary>
        string Serialize(object data);
    }
}
