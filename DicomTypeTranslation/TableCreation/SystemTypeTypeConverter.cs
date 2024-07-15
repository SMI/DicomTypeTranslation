using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DicomTypeTranslation.TableCreation;

internal sealed class SystemTypeTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(Type).IsAssignableFrom(type);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer _)
    {
        var scalar = parser.Consume<Scalar>();
        return Type.GetType(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object value, Type _1, ObjectSerializer _2)
    {
        var typeName = (value as Type)?.FullName ?? throw new ArgumentException("SytemTypeTypeConverter.WriteYaml called with non-Type argument",nameof(value));
        emitter.Emit(new Scalar(typeName));
    }
}