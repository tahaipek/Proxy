﻿using Microsoft.AspNetCore.Http;
using NetCoreStack.Proxy.Internal;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace NetCoreStack.Proxy
{
    public class HttpDispatchProxy : DispatchProxyAsync
    {
        private ProxyContext _proxyContext;

        private IProxyManager _proxyManager;

        public HttpDispatchProxy()
        {
        }

        private object CreateContextProperties(RequestContext context, MethodInfo targetMethod)
        {
            var properties = new
            {
                EnvironmentName = Environment.MachineName,
                Message = $"Proxy call from Sandbox: {Environment.MachineName} to API method: {targetMethod?.Name}",
                Method = context?.Request?.Method,
                Authority = context?.Request?.RequestUri?.Authority,
                LocalPath = context?.Request?.RequestUri?.LocalPath,
                RequestRegion = context?.RegionKey
            };

            return properties;
        }

        /// <summary>
        /// Call after resolve the proxy
        /// </summary>
        /// <param name="proxyFactory"></param>
        public void Initialize(ProxyContext context, IProxyManager proxyManager)
        {
            _proxyContext = context;
            _proxyManager = proxyManager;
        }

        private async Task<ResponseContext> InternalInvokeAsync(MethodInfo targetMethod, object[] args, Type genericReturnType = null)
        {
            var culture = _proxyContext.CultureFactory?.Invoke();

            var descriptor = new RequestDescriptor(targetMethod,
                _proxyContext.ProxyType,
                _proxyContext.ClientIp,
                _proxyContext.UserAgent,
                _proxyContext.Query,
                culture,
                args);

            RequestContext requestContext = await _proxyManager.CreateRequestAsync(descriptor);
            ResponseContext responseContext = null;
            try
            {
                HttpResponseMessage response = null;
                string metadata = string.Empty;

                if (_proxyManager.HasFilter)
                {
                    await Task.WhenAll(_proxyManager.RequestFilters.Select(t => t.InvokeAsync(requestContext)));
                }
                
                response = await _proxyManager.HttpClient.SendAsync(requestContext.Request);
                responseContext = await ProxyResultExecutor.ExecuteAsync(response,
                    requestContext,
                    genericReturnType);

            }
            catch (Exception ex)
            {
                var properties = CreateContextProperties(requestContext, targetMethod);
                var result = properties.GetConfigurationContextDetail(properties);
                throw new ProxyException(result, ex);
            }

            if ((int)responseContext.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                var properties = CreateContextProperties(requestContext, targetMethod);
                var result = properties.GetConfigurationContextDetail(properties);
                throw new ProxyException(result, new HttpRequestException());
            }

            return responseContext;
        }

        public override async Task InvokeAsync(MethodInfo method, object[] args)
        {
            if (method.Name == "get_ControllerContext")
            {
                throw new NotSupportedException($"\"ActionContext\" is not supported for proxy instance");
            }

            using (ResponseContext responseContext = await InternalInvokeAsync(method, args)) { }  
        }

        public override async Task<T> InvokeAsyncT<T>(MethodInfo method, object[] args)
        {
            if (method.Name == "get_ControllerContext")
            {
                throw new NotSupportedException($"\"ActionContext\" is not supported for proxy instance");
            }

            using (ResponseContext responseContext = await InternalInvokeAsync(method, args, typeof(T)))
            {
                return (T)responseContext.Value;
            }
        }

        public override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == "get_ControllerContext")
            {
                throw new NotSupportedException($"\"ActionContext\" is not supported for proxy instance");
            }

            ResponseContext context = Task.Run(async () => await InternalInvokeAsync(method, args).ConfigureAwait(false)).Result;
            return context.Value;
        }
    }
}
