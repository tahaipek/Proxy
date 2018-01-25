﻿using NetCoreStack.Proxy.Extensions;
using System.Net.Http;

namespace NetCoreStack.Proxy
{
    public class HttpGetContentBinder : ContentModelBinder
    {
        public HttpGetContentBinder(HttpMethod httpMethod)
            : base(httpMethod)
        {

        }

        public override void BindContent(ContentModelBindingContext bindingContext)
        {
            EnsureTemplateResult ensureTemplateResult = EnsureTemplate(bindingContext);
            if (ensureTemplateResult.BindingCompleted)
                return;

            ModelDictionaryResult result = bindingContext.ModelContentResolver.Resolve(bindingContext.Parameters,
                bindingContext.Args,
                ensureTemplateResult.ParameterOffset,
                ensureTemplateResult.IgnoreModelPrefix);

            bindingContext.TryUpdateUri(result.Dictionary);
        }
    }
}
