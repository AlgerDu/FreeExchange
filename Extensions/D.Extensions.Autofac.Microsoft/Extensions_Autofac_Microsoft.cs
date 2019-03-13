using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace D.Extensions
{
    /// <summary>
    /// 使用 autofac 时，有关 Microsoft.Extensions 的一些扩展
    /// </summary>
    public static class Extensions_Autofac_Microsoft
    {
        /// <summary>
        /// 添加 Microsoft.Extensions 相关的一些组件
        /// </summary>
        /// <param name="builder"></param>
        public static void AddMicrosoftExtensions(this ContainerBuilder builder)
        {
            var service = new ServiceCollection();
            service.AddLogging();
            service.AddOptions();

            builder.Populate(service);
        }
    }
}
