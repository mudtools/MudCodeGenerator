using System;

class Program
{
    static void Main()
    {
        // 带命名空间的测试
        var name = RemoveInterfacePrefix("ComObjectWrapTest.IWordApplication?");
        Console.WriteLine($"ComObjectWrapTest.IWordApplication? -> {name}");
        // → "ComObjectWrapTest.WordApplication?"

        name = RemoveInterfacePrefix("System.Collections.Generic.IList<string>");
        Console.WriteLine($"System.Collections.Generic.IList<string> -> {name}");
        // → "System.Collections.Generic.List<string>"

        name = RemoveInterfacePrefix("MyNamespace.SubNamespace.IService");
        Console.WriteLine($"MyNamespace.SubNamespace.IService -> {name}");
        // → "MyNamespace.SubNamespace.Service"

        // 嵌套类测试
        name = RemoveInterfacePrefix("OuterClass+IInnerClass");
        Console.WriteLine($"OuterClass+IInnerClass -> {name}");
        // → "OuterClass+InnerClass"

        name = RemoveInterfacePrefix("ComObjectWrapTest.Outer+IInner?");
        Console.WriteLine($"ComObjectWrapTest.Outer+IInner? -> {name}");
        // → "ComObjectWrapTest.Outer+Inner?"

        // 边界情况
        name = RemoveInterfacePrefix("IA");
        Console.WriteLine($"IA -> {name}");
        // → "A"

        name = RemoveInterfacePrefix("I");
        Console.WriteLine($"I -> {name}");
        // → "I"

        name = RemoveInterfacePrefix("item");
        Console.WriteLine($"item -> {name}");
        // → "item" (小写开头，不处理)

        name = RemoveInterfacePrefix("Iinterface");
        Console.WriteLine($"Iinterface -> {name}");
        // → "interface" (第二个字符小写，不处理)

        name = RemoveInterfacePrefix("IEnumerable");
        Console.WriteLine($"IEnumerable -> {name}");
        // → "IEnumerable" (第二个字符小写，不处理)

        name = RemoveInterfacePrefix("IList");
        Console.WriteLine($"IList -> {name}");
        // → "List" (第二个字符大写，处理)

        name = RemoveInterfacePrefix("ICollection");
        Console.WriteLine($"ICollection -> {name}");
        // → "Collection" (第二个字符大写，处理)


        /// <summary>
        /// 移除接口名前的"I"前缀
        /// </summary>
        /// <returns>去掉"I"前缀的类型名</returns>
        static string RemoveInterfacePrefix(string interfaceTypeName)
        {
            if (string.IsNullOrEmpty(interfaceTypeName))
                return interfaceTypeName;

            // 处理可空类型
            if (interfaceTypeName.EndsWith("?", StringComparison.Ordinal))
            {
                // 移除末尾的'?'，递归处理内部类型
                var nonNullType = interfaceTypeName.Substring(0, interfaceTypeName.Length - 1);
                var processedType = RemoveInterfacePrefix(nonNullType);
                return processedType + "?";
            }

            // 处理数组类型
            if (interfaceTypeName.EndsWith("[]", StringComparison.Ordinal))
            {
                var elementType = interfaceTypeName.Substring(0, interfaceTypeName.Length - 2);
                var processedType = RemoveInterfacePrefix(elementType);
                return processedType + "[]";
            }

            // 分割命名空间和类型名
            var lastDotIndex = interfaceTypeName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                // 有命名空间的情况
                var namespacePart = interfaceTypeName.Substring(0, lastDotIndex + 1); // 包含点
                var typeNamePart = interfaceTypeName.Substring(lastDotIndex + 1);

                // 只对类型名部分进行处理
                var processedTypeName = RemoveInterfacePrefixFromTypeName(typeNamePart);

                return namespacePart + processedTypeName;
            }
            else
            {
                // 没有命名空间，直接处理类型名
                return RemoveInterfacePrefixFromTypeName(interfaceTypeName);
            }
        }

        /// <summary>
        /// 从类型名中移除"I"前缀（不处理命名空间和可空符号）
        /// </summary>
        static string RemoveInterfacePrefixFromTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || typeName.Length < 2)
                return typeName;

            // 处理嵌套类的情况（例如 "Outer+IInner"）
            var plusIndex = typeName.IndexOf('+');
            if (plusIndex >= 0)
            {
                // 分离外部类和嵌套类
                var outerClassName = typeName.Substring(0, plusIndex);
                var innerClassName = typeName.Substring(plusIndex + 1);

                // 递归处理两部分
                var processedOuter = RemoveInterfacePrefixFromTypeName(outerClassName);
                var processedInner = RemoveInterfacePrefixFromTypeName(innerClassName);

                return processedOuter + "+" + processedInner;
            }

            // 处理泛型类型参数（例如 "IList<T>"）
            var angleBracketIndex = typeName.IndexOf('<');
            if (angleBracketIndex >= 0)
            {
                // 分离类型名和泛型参数
                var baseTypeName = typeName.Substring(0, angleBracketIndex);
                var genericParams = typeName.Substring(angleBracketIndex);

                // 只对基础类型名进行处理
                var processedBase = RemoveInterfacePrefixFromTypeName(baseTypeName);

                return processedBase + genericParams;
            }

            // 核心逻辑：移除"I"前缀
            // 条件：以'I'开头，长度至少为2，第二个字符是大写
            if (typeName[0] == 'I' && char.IsUpper(typeName[1]))
            {
                // 检查是否是特殊情况，如"IEnumerable"等.NET内置接口
                // 这些接口虽然符合"I"+大写规则，但我们不应该移除它们的前缀
                string[] specialCases = { "IEnumerable", "IEnumerator", "IEqualityComparer", "IComparable", "IEquatable" };
                
                if (specialCases.Contains(typeName))
                {
                    return typeName;
                }
                
                // 标准接口前缀，移除 'I'
                return typeName.Substring(1);
            }

            return typeName;
        }
    }
}
