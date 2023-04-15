﻿using Dumpify.Config;
using Dumpify.Descriptors;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections;

namespace Dumpify.Renderers.Spectre.Console.TableRenderer.CustomTypeRenderers;
internal class DictionaryTypeRenderer : ICustomTypeRenderer<IRenderable>
{
    private readonly IRendererHandler<IRenderable> _handler;

    public Type DescriptorType { get; } = typeof(MultiValueDescriptor);

    public DictionaryTypeRenderer(IRendererHandler<IRenderable> handler)
    {
        _handler = handler;
    }

    public IRenderable Render(IDescriptor descriptor, object obj, RenderContext context)
    {
        var table = new Table();

        var colorConfig = context.Config.ColorConfig;

        var title = "Dictionary";
        table.AddColumn(new TableColumn(new Markup("Key", new Style(foreground: colorConfig.ColumnNameColor.ToSpectreColor()))));
        table.AddColumn(new TableColumn(new Markup("Value", new Style(foreground: colorConfig.ColumnNameColor.ToSpectreColor()))));

        if(context.Config.TableConfig.ShowTableHeaders is not true)
        {
            table.HideHeaders();
        }

        var dictionary = (IDictionary)obj;

        Type? keyType = null;
        Type? valueType = null;

        var memberProvider = context.Config.MemberProvider;

        foreach (var keyValue in dictionary.Keys)
        {
            keyType = keyValue.GetType();

            var keyDescriptor = DumpConfig.Default.Generator.Generate(keyType, null, context.Config.MemberProvider);
            var keyRenderable = _handler.RenderDescriptor(keyValue, keyDescriptor, context);

            var value = dictionary[keyValue];

            if(value is not null && valueType is null)
            {
                valueType = value.GetType();
            }

            var valueRenderable = value switch
            {
                null => _handler.RenderNullValue(null, context),
                not null => _handler.RenderDescriptor(value, DumpConfig.Default.Generator.Generate(value.GetType(), null, memberProvider), context),
            };

            table.AddRow(keyRenderable, valueRenderable);
        }

        if(keyType is not null && valueType is not null)
        {
            title = $"{title}<{keyType.Name}, {valueType.Name}>";
        }
        
        if(context.Config.TypeNamingConfig.ShowTypeNames is true)
        {
            table.Title = new TableTitle(Markup.Escape(title), new Style(foreground: colorConfig.TypeNameColor.ToSpectreColor()));
        }

        return table.Collapse();
    }

    public bool ShouldHandle(IDescriptor descriptor, object obj)
        => descriptor.Type.IsGenericType && descriptor.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
}