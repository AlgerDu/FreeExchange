using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Autofac;
using D.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

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
        ILifetimeScope _scope;

        Dictionary<string, List<ActionItems>> _urlToActions;

        public MvcActionExecutor(
            ILogger<MvcActionExecutor> logger
            , IOptions<MvcActionExecutorOptions> options
            , ILifetimeScope scope
            )
        {
            _logger = logger;
            _options = options.Value;
            _scope = scope;

            AnaylseActions();
            DiAllControllerType();
        }

        public IResult<object> InvokeAction(IActionExecuteMessage msg)
        {
            var url = msg.Url;

            var items = _urlToActions[url];

            if (items == null)
            {
                return CreateError(ExchangeCode.ActionErrorType);
            }

            var paramsJson = msg.Params[0] as string;

            ActionItems actionItem = null;
            object[] actionParams = null;

            foreach (var item in items)
            {
                var tryRst = TryResoleActionParams(item.Action.GetParameters(), paramsJson);

                if (tryRst.IsSuccess())
                {
                    actionItem = item;
                    actionParams = tryRst.Data;
                    break;
                }
            }

            if (actionItem != null)
            {
                var controller = _scope.Resolve(actionItem.ControllerType);

                PreDealController(controller, msg.Proxy);

                var actionRst = actionItem.Action.Invoke(controller, actionParams);

                return Result.CreateSuccess<object>(actionRst);
            }
            else
            {
                return CreateError(ExchangeCode.ActionErrorType);
            }

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
                    if (IsOkClassType(type))
                    {
                        var ctlName = type.Name.ToLower();
                        ctlName = ctlName.EndsWith("controller")
                            ? ctlName.Remove(ctlName.Length - 10, 10) : ctlName;

                        foreach (var m in type.GetMethods(
                            BindingFlags.Public
                            | BindingFlags.Instance))
                        {
                            if (!m.IsSpecialName
                                && m.DeclaringType != typeof(object)
                                )
                            {
                                var item = new ActionItems
                                {
                                    Action = m,
                                    ControllerType = type,
                                    Url = $"{ctlName}/{m.Name.ToLower()}"
                                };

                                if (_urlToActions.ContainsKey(item.Url))
                                {
                                    _urlToActions[item.Url].Add(item);
                                }
                                else
                                {
                                    _urlToActions.Add(item.Url, new List<ActionItems>() { item });
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsOkClassType(Type ctlType)
        {
            var ctlInterface = typeof(IExchangeController);

            if (ctlType.IsPublic
                        && ctlType.IsClass
                        && !ctlType.IsAbstract
                        && ctlInterface.IsAssignableFrom(ctlType))
            {
                foreach (var namesapce in _options.Namespaces)
                {
                    if (ctlType.Namespace.StartsWith(namesapce))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void DiAllControllerType()
        {
            _scope = _scope.BeginLifetimeScope((builder) =>
            {
                foreach (var items in _urlToActions.Values)
                {
                    foreach (var item in items)
                    {
                        builder.RegisterType(item.ControllerType);
                    }
                }
            });
        }

        private IResult<Object> CreateError(ExchangeCode code)
        {
            return new Result<object>
            {
                Code = (int)code
            };
        }

        private IResult<object[]> TryResoleActionParams(ParameterInfo[] parameters, string json)
        {
            var jarray = JArray.Parse(json);

            List<object> rst = new List<object>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var ptype = parameters[i].ParameterType;
                var jobject = jarray[i];

                rst.Add(jobject.ToObject(ptype));
            }

            return Result.CreateSuccess<object[]>(rst.ToArray());
        }

        private void PreDealController(object controller, IExchangeProxy proxy)
        {
            var p = controller.GetType().GetProperty("Client");
            p.SetValue(controller, proxy);
        }
    }
}
