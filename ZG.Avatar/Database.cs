using UnityEngine;
using System;

namespace ZG.Avatar
{
    public class Database : ScriptableObject
    {
        [Serializable]
        public struct Condition
        {
            public string label;

            public bool isNegative;
        }

        [Serializable]
        public struct Info
        {
            public string avatarName;
            public bool isNegative;
            public Condition[] conditions;
        }

        [Serializable]
        public struct Part
        {
            //public string name;
            public string label;
            public string group;
            public Info[] infos;
        }
        
        [Serializable]
        public struct MaterialOption
        {
            public string name;

            [Tooltip("对应预制体上第几个材质")]
            public int index;

            [Tooltip("颜色随机种子")]
            public int seed;

            [ColorUsage(true, true)]
            public Color minColor;
            [ColorUsage(true, true)]
            public Color maxColor;
            public string colorParameterName;

            public string textureLabel;
            public string textureName;
            public string textureParameterName;
        }

        [Serializable]
        public struct PartOption
        {
            public string name;

            public string partName;
        }

        [Serializable]
        public struct Group
        {
            public string name;

            public string[] options;
        }

        [Serializable]
        public struct Node
        {
            public MaterialOption[] materialOptions;

            public PartOption[] partOptions;

            public Group[] groups;

            public int defaultValue
            {
                get
                {

                    int numGroups = groups == null ? 0 : groups.Length;
                    if (numGroups < 1)
                        return 0;

                    int value = 0, bits = 0, length;
                    Group group;
                    for (int i = 0; i < numGroups; ++i)
                    {
                        group = groups[i];

                        length = group.options == null ? 0 : group.options.Length;
                        if (length > 0)
                        {
                            value |= 1 << bits;

                            bits += ((uint)length).GetHighestBit();
                        }
                    }

                    return value;
                }
            }

            public int randValue
            {
                get
                {

                    int numGroups = groups == null ? 0 : groups.Length;
                    if (numGroups < 1)
                        return 0;

                    int value = 0, bits = 0, length;
                    Group group;
                    for (int i = 0; i < numGroups; ++i)
                    {
                        group = groups[i];

                        length = group.options == null ? 0 : group.options.Length;
                        if (length > 0)
                        {
                            value |= UnityEngine.Random.Range(0, length) << bits;

                            bits += ((uint)length).GetHighestBit();
                        }
                    }

                    return value;
                }
            }

            /*public int Normalize(int value)
            {
                int numGroups = groups == null ? 0 : groups.Length;
                if (numGroups < 1)
                    return 0;

                int normalizedValue = 0, bits = 0, bit, length, count;
                Group group;
                for (int i = 0; i < numGroups; ++i)
                {
                    group = groups[i];

                    length = group.options == null ? 0 : group.options.Length;
                    if (length > 0)
                    {
                        bit = ((uint)(length - 1)).GetHighestBit();

                        count = 1 << bit;

                        normalizedValue |= Mathf.RoundToInt((value & (count - 1)) * 1.0f / length * count) << bits;

                        value >>= bit;

                        bits += bit;
                    }
                }

                return normalizedValue;
            }
            
            public int Convert(int normalizedValue)
            {
                int numGroups = groups == null ? 0 : groups.Length;
                if (numGroups < 1)
                    return 0;

                int value = 0, bits = 0, bit, length, count;
                Group group;
                for (int i = 0; i < numGroups; ++i)
                {
                    group = groups[i];

                    length = group.options == null ? 0 : group.options.Length;
                    if (length > 0)
                    {
                        bit = ((uint)(length - 1)).GetHighestBit();

                        count = 1 << bit;

                        value |= Mathf.FloorToInt((normalizedValue & (count - 1)) * 1.0f / count * length) << bits;

                        normalizedValue >>= bit;

                        bits += bit;
                    }
                }

                return value;
            }*/

            public int Set(int value, int groupIndex, int optionIndex, out string option)
            {
                option = null;
                if (optionIndex < 0 || groupIndex < 0 || groups == null || groups.Length <= groupIndex)
                    return 0;

                Group result = groups[groupIndex];
                int length = result.options == null ? 0 : result.options.Length;
                if (length <= optionIndex)
                    return 0;

                option = result.options[optionIndex];

                int mask = (1 << ((uint)length).GetHighestBit()) - 1, bits = 0;
                foreach (var group in groups)
                {
                    if (--groupIndex < 0)
                        break;

                    length = group.options == null ? 0 : group.options.Length;
                    if(length > 0)
                        bits += ((uint)length).GetHighestBit();
                }

                value &= ~(mask << bits);
                return value | (optionIndex << bits);
            }

            public string Get(int value, int groupIndex)
            {
                if (groupIndex < 0 || groups == null || groups.Length <= groupIndex)
                    return null;

                Group result = groups[groupIndex];
                int length = result.options == null ? 0 : result.options.Length;
                if (length < 1)
                    return null;

                int mask = (1 << ((uint)length).GetHighestBit()) - 1, bits = 0;
                foreach (var group in groups)
                {
                    if (--groupIndex < 0)
                        break;

                    length = group.options == null ? 0 : group.options.Length;
                    if (length > 0)
                        bits += ((uint)length).GetHighestBit();
                }

                return result.options[(value >> bits) & mask];
            }

            public void Get(int value, Action<string> options)
            {
                if (options == null)
                    return;

                int numGroups = groups == null ? 0 : groups.Length;
                if (numGroups < 1)
                    return;

                int length, index, bit, bits = 0;
                Group group;
                for (int i = 0; i < numGroups; ++i)
                {
                    group = groups[i];
                    length = group.options == null ? 0 : group.options.Length;
                    if (length > 0)
                    {
                        bit = ((uint)length).GetHighestBit();

                        index = ((1 << bit) - 1) & (value >> bits);
                        options(group.options[index < length ? index : index - length]);
                        
                        bits += bit;
                    }
                }
            }

            public int GetOptionIndexOf(int value, int groupIndex)
            {
                if (groupIndex < 0 || groups == null || groups.Length <= groupIndex)
                    return -1;

                Group result = groups[groupIndex];
                int length = result.options == null ? 0 : result.options.Length;
                if (length < 1)
                    return -1;

                int mask = (1 << ((uint)length).GetHighestBit()) - 1, bits = 0;
                foreach (var group in groups)
                {
                    if (--groupIndex < 0)
                        break;

                    length = group.options == null ? 0 : group.options.Length;
                    if (length > 0)
                        bits += ((uint)length).GetHighestBit();
                }

                return (value >> bits) & mask;
            }

            public void Select(string option, Action<MaterialOption> materialOptions, Action<PartOption> partOptions)
            {
                if (materialOptions != null && this.materialOptions != null)
                {
                    foreach (var materialOption in this.materialOptions)
                    {
                        if (materialOption.name == option)
                            materialOptions(materialOption);
                    }
                }

                if (partOptions != null && this.partOptions != null)
                {
                    foreach (var partOption in this.partOptions)
                    {
                        if (partOption.name == option)
                            partOptions(partOption);
                    }
                }
            }
        }

        [Serializable]
        public class Parts : Map<Part>
        {

        }

        [Serializable]
        public class Nodes : Map<Node>
        {

        }

        [Map]
        public Parts parts;

        [Map]
        public Nodes nodes;
    }
}