using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// URL 경로 파라미터. {name} 플레이스홀더를 치환한다.
    /// [Get("/users/{id}")] void GetUser([Path("id")] int userId)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PathAttribute : Attribute
    {
        public string Name { get; }
        public PathAttribute(string name = null) { Name = name; }
    }
}
