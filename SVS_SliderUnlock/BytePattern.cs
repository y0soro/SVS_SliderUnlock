using System;
using System.IO;
using System.Linq;

namespace SVS_SliderUnlock;

internal class BytePattern
{
    // (pattern byte, mask)
    private readonly (byte, byte)[] pattern;

    public BytePattern(string pattern)
    {
        static (byte, byte) ConvertMatcher(string matcher)
        {
            byte pat = 0;
            byte mask = 0;

            byte baseBits = (byte)(matcher.Length > 2 ? 1 : 4);
            byte baseMask = (byte)(matcher.Length > 2 ? 1 : 0xf);

            for (int i = 0; i < matcher.Length; i++)
            {
                var ch = matcher.Substring(matcher.Length - i - 1, 1);
                if (ch == "?")
                    continue;

                var num = Convert.ToByte(ch, baseMask + 1);
                pat = (byte)(pat | (num << (baseBits * i)));

                mask = (byte)(mask | (baseMask << (baseBits * i)));
            }

            return ((byte)(pat & mask), mask);
        }

        this.pattern = pattern
            .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
            .Select(ConvertMatcher)
            .ToArray();
    }

    public bool Search(Stream stream, out long pos)
    {
        for (var i = 0; i < pattern.Length; )
        {
            var matcher = pattern[i];
            var b = stream.ReadByte();
            if (b < 0)
            {
                pos = -1;
                return false;
            }

            if (matcher.Item1 != (b & matcher.Item2))
            {
                // go to next search iteration
                stream.Position -= i;
                i = 0;
                continue;
            }

            // keep matching pattern
            i++;
        }

        pos = stream.Position - pattern.Length;
        return true;
    }
}
