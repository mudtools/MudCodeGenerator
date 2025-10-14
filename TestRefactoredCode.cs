using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mud.EntityCodeGenerator;
using Mud.EntityCodeGenerator.Helper;

namespace MudCodeGenerator.Test
{
    /// <summary>
    /// 测试重构后的代码生成器
    /// </summary>
    public class TestRefactoredCode
    {
        public static void TestConfigurationManager()
        {
            Console.WriteLine("=== 测试配置管理器 ===");
            
            var config = new GeneratorConfiguration();
            
            // 测试默认值
            Console.WriteLine($"EntityAttachAttributes: {config.EntityAttachAttributes?.Length ?? 0}");
            Console.WriteLine($"VoAttributes: {config.VoAttributes?.Length ?? 0}");
            Console.WriteLine($"BoAttributes: {config.BoAttributes?.Length ?? 0}");
            Console.WriteLine($"EntityPrefix: {config.EntityPrefix}");
            
            // 测试合并功能
            var defaultAttrs = new[] { "Required", "StringLength" };
            var merged = config.MergePropertyAttributes(defaultAttrs, "bo");
            Console.WriteLine($"合并后的属性数量: {merged?.Length ?? 0}");
            
            Console.WriteLine("配置管理器测试完成\n");
        }
        
        public static void TestPropertyGeneratorFactory()
        {
            Console.WriteLine("=== 测试属性生成器工厂 ===");
            
            // 创建一个模拟的生成器
            var mockGenerator = new MockDtoGenerator();
            
            // 测试创建属性生成器
            var propertyGenerator = PropertyGeneratorFactory.CreatePropertyGenerator(mockGenerator, true, true);
            Console.WriteLine("属性生成器创建成功");
            
            var fieldGenerator = PropertyGeneratorFactory.CreateFieldGenerator(mockGenerator, true, true, false);
            Console.WriteLine("字段生成器创建成功");
            
            Console.WriteLine("属性生成器工厂测试完成\n");
        }
        
        public static void TestErrorHandler()
        {
            Console.WriteLine("=== 测试错误处理器 ===");
            
            // 测试安全执行
            bool executed = false;
            ErrorHandler.SafeExecute(null, "TestClass", () =>
            {
                executed = true;
                Console.WriteLine("安全执行测试成功");
            });
            
            Console.WriteLine($"执行状态: {executed}");
            
            // 测试异常处理
            ErrorHandler.SafeExecute(null, "TestClass", () =>
            {
                throw new Exception("测试异常");
            });
            
            Console.WriteLine("异常处理测试完成\n");
        }
        
        public static void Main()
        {
            Console.WriteLine("开始测试重构后的代码...\n");
            
            TestConfigurationManager();
            TestPropertyGeneratorFactory();
            TestErrorHandler();
            
            Console.WriteLine("所有测试完成！");
        }
    }
    
    /// <summary>
    /// 模拟的DTO生成器用于测试
    /// </summary>
    public class MockDtoGenerator : TransitiveDtoGenerator
    {
        protected override void GenerateCode(SourceProductionContext context, Compilation compilation, ClassDeclarationSyntax orgClassDeclaration)
        {
            // 空实现用于测试
        }
        
        public new bool IsIgnoreGenerator<T>(T member) where T : MemberDeclarationSyntax
        {
            return false;
        }
        
        public new bool IsPrimary<T>(T member) where T : MemberDeclarationSyntax
        {
            return false;
        }
        
        public new PropertyDeclarationSyntax BuildProperty(PropertyDeclarationSyntax member)
        {
            return member;
        }
        
        public new PropertyDeclarationSyntax BuildProperty(FieldDeclarationSyntax member, bool isProperty = true)
        {
            // 创建一个简单的属性声明用于测试
            var property = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                "TestProperty");
                
            return property;
        }
    }
}