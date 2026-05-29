using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// 파라미터 레벨 동적 헤더.
    /// [Get("/users")] void Get([Header("Authorization")] string token)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class HeaderAttribute : Attribute
    {
        public string Name { get; }
        public HeaderAttribute(string name) { Name = name; }
    }
}
