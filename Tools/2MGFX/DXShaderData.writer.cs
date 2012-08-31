using System.IO;
using TwoMGFX;

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class DXShaderData
    {
        public void Write(BinaryWriter writer, Options options)
        {
            writer.Write(IsVertexShader);

            writer.Write((ushort)ShaderCode.Length);
            writer.Write(ShaderCode);

            writer.Write((byte)_samplers.Length);
            foreach (var sampler in _samplers)
            {
                writer.Write((byte)sampler.type);
                writer.Write((byte)sampler.index);

                if (!options.DX11Profile)
                    writer.Write(sampler.samplerName);

                writer.Write((byte)sampler.parameter);
            }

            writer.Write((byte)_cbuffers.Length);
            foreach (var cb in _cbuffers)
                writer.Write((byte)cb);

            if (options.DX11Profile)
                return;

            // The rest of this is for GL only!

            writer.Write((byte)_attributes.Length);
            foreach (var attrib in _attributes)
            {
                writer.Write(attrib.name);
                writer.Write((byte)attrib.usage);
                writer.Write((byte)attrib.index);
                writer.Write(attrib.format);
            }
        }
    }
}