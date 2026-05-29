using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// application/x-www-form-urlencoded 폼 필드로 전송한다.
    /// name 생략 시 파라미터 이름을 키로 사용한다.
    /// <example>[Post("login")] Task LoginAsync([Field("user")] string u, [Field("pw")] string pw);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class FieldAttribute : Attribute
    {
        public string Name { get; }
        public FieldAttribute(string name = null) { Name = name; }
    }
}
