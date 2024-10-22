using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Python
{
    public static class Utils
    {
        public static void Assert(bool ok)
        {
            if (!ok)
            {
                throw new CsharpException("Assertion failed");
            }
        }

        public static void Assert(bool ok, string msg)
        {
            if (!ok)
            {
                throw new CsharpException(msg);
            }
        }

        public static string LoadPythonLib(string key)
        {
            var t = Resources.Load<TextAsset>("PythonLib/" + key);
            Utils.Assert(t != null, $"{key}.txt not found");
            return t.text;
        }

        public static string LoadTestCase(string path)
        {
            Assert(File.Exists(path), $"{path} not found");
            return File.ReadAllText(path);
        }

        public static T Cast<T>(object value) where T : struct
        {
            if (typeof(T) != typeof(int) &&
                typeof(T) != typeof(ushort) &&
                typeof(T) != typeof(float) &&
                typeof(T) != typeof(double))
            {
                throw new ArgumentException(
                    string.Format("Type '{0}' is not valid.", typeof(T).ToString()));
            }

            if (value is ushort) return (T)Convert.ChangeType((ushort)value, typeof(T));
            if (value is int) return (T)Convert.ChangeType((int)value, typeof(T));
            if (value is float) return (T)Convert.ChangeType((float)value, typeof(T));
            if (value is double) return (T)Convert.ChangeType((double)value, typeof(T));

            throw new ArgumentException(
                string.Format("Type '{0}' is not valid.", value.GetType().ToString()));
        }

        public static string Base64Encode(string plainText)
        {
            // var plainTextBytes = plainText.ToCharArray().Select(c => (byte)c).ToArray();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            // return new string(base64EncodedBytes.Select(b => (char)b).ToArray());
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        //cpython does not encode [\x7f - \xa0], but we do.
        //but some of UTF-8 bytes in this range need to be re-mapped.
        private static readonly Dictionary<char, byte> _charMapping = new Dictionary<char, byte>()
        {
            {(char)8364, (byte)128},
            {(char)8218, (byte)130},
            {(char)402, (byte)131},
            {(char)8222, (byte)132},
            {(char)8230, (byte)133},
            {(char)8224, (byte)134},
            {(char)8225, (byte)135},
            {(char)710, (byte)136},
            {(char)8240, (byte)137},
            {(char)352, (byte)138},
            {(char)8249, (byte)139},
            {(char)338, (byte)140},
            {(char)381, (byte)142},
            {(char)8216, (byte)145},
            {(char)8217, (byte)146},
            {(char)8220, (byte)147},
            {(char)8221, (byte)148},
            {(char)8226, (byte)149},
            {(char)8211, (byte)150},
            {(char)8212, (byte)151},
            {(char)732, (byte)152},
            {(char)8482, (byte)153},
            {(char)353, (byte)154},
            {(char)8250, (byte)155},
            {(char)339, (byte)156},
            {(char)382, (byte)158},
            {(char)376, (byte)159},
        };

        public static byte RemapChar(VM vm, char c)
        {
            if (_charMapping.TryGetValue(c, out byte v))
                return v;

            if ((int)c > byte.MaxValue || (int)c < byte.MinValue)
            {
                vm.SyntaxError("bytes can only contain ASCII literal characters.");
            }

            return (byte)c;
        }

        private static string pattern = @"%(?<Number>\d*(\.\d+)?)?(?<Type>d|f|s)";
        private static Regex pattern2 = new Regex(@"%[+\-0-9]*\.*([0-9]*)([xXeEfFdDgG])");

        public static string FormatCStyleString(VM vm, string str, List<object> @params)
        {
            int offset = 0;
            int count = 0;

            var caputred = Regex.Matches(str, pattern);
            if (caputred.Count < @params.Count)
            {
                vm.TypeError("not all arguments converted during string formatting");
                return null;
            }
            if (caputred.Count > @params.Count)
            {
                vm.TypeError("not enough arguments for format string");
                return null;
            }

            foreach (Match match in caputred)
            {
                var groups = match.Groups;
                string specification = groups[0].Value;
                int index = groups[0].Index;
                string s1 = specification.Replace("%%", "%").Replace("{", "{{").Replace("}", "}}");
                string formatted = "";
                if (s1.Contains("%s"))
                {
                    string s2 = s1.Replace("%s", "{0}");
                    formatted = String.Format(s2, vm.PyStr(@params[count]));
                }
                else
                {
                    string s2 = pattern2.Replace(s1, m =>
                    {
                        if (m.Groups.Count == 3)
                        {
                            switch (m.Groups[2].Value)
                            {
                                case "d":
                                    {
                                        if (!(@params[count] is int || @params[count] is float))
                                        {
                                            vm.TypeError("%d format: a number is required");
                                        }
                                        if (@params[count] is float f)
                                            @params[count] = (int)f;
                                        break;
                                    }
                                case "f":
                                    {
                                        if (!(@params[count] is int || @params[count] is float))
                                        {
                                            vm.TypeError("%f format: must be real number");
                                        }
                                        if (string.IsNullOrEmpty(m.Groups[1].Value))
                                        {
                                            //set default decimal places to 6
                                            return "{0:f6}";
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        vm.ValueError($"unsupported format character {m.Groups[2].Value}");
                                        return null;
                                    }
                            }
                            return "{0:" + m.Groups[2].Value + m.Groups[1].Value + "}";
                        }
                        return "{0}";
                    });
                    if (!string.IsNullOrEmpty(s2))
                        formatted = String.Format(s2, @params[count]);
                }
                str = str.Remove(index + offset, specification.Length).Insert(index + offset, formatted);
                offset += formatted.Length - specification.Length;
                count++;
            }
            return str;
        }
    }
}
