using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using D.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D.FreeExchange.Core
{
    /// <summary>
    /// action 项
    /// </summary>
    internal class ActionItems
    {
        public string Url { get; set; }

        public Type ControllerType { get; set; }

        public MethodInfo Action { get; set; }
    }

    /// <summary>
    /// 类 mcv action 执行
    /// </summary>
    public class MvcActionExecutor : IActionExecutor
    {
        ILogger _logger;
        MvcActionExecutorOptions _options;

        Dictionary<string, List<ActionItems>> _urlToActions;

        public MvcActionExecutor(
            ILogger<MvcActionExecutor> logger
            , IOptions<MvcActionExecutorOptions> options
            )
        {
            _logger = logger;
            _options = options.Value;

            AnaylseActions();
        }

        public IResult<object> InvokeAction(IExchangeMessage msg, IExchangeClientProxy clientProxy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 预解析所有的 action
        /// </summary>
        private void AnaylseActions()
        {
            _urlToActions = new Dictionary<string, List<ActionItems>>();

            var controllerType = typeof(IExchangeController);

            foreach (var aName in _options.AssemblyNames)
            {
                var assembly = Assembly.Load(aName);

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsPublic
                        && type.IsClass
                        && !type.IsAbstract
                        && controllerType.IsAssignableFrom(type))
                    {

                    }
                }
            }
        }
    }
}
