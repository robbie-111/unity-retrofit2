namespace Unitrofit.Converter
{
    /// <summary>
    /// 요청/응답 직렬화 계약.
    /// 기본 구현은 JsonConverter (Newtonsoft.Json).
    /// </summary>
    public interface IConverter
    {
        /// <summary>JSON 문자열을 T 타입으로 역직렬화한다.</summary>
        T FromBody<T>(string body);

        /// <summary>객체를 JSON 문자열로 직렬화한다.</summary>
        string ToBody(object data);
    }
}
