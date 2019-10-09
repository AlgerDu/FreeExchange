using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// MvcActionExecutor 选项
    /// </summary>
    public class MvcActionExecutorOptions
    {
        public string[] Namespaces { get; set; }

        public string[] AssemblyNames { get; set; }
    }
}
