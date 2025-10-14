using Microsoft.CodeAnalysis;
using Mud.EntityCodeGenerator.Diagnostics;
using System;

namespace Mud.EntityCodeGenerator.Helper;

/// <summary>
/// 错误处理器
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// 安全执行代码生成操作
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="className">类名</param>
    /// <param name="action">要执行的操作</param>
    /// <param name="failureDescriptor">失败描述符</param>
    /// <param name="errorDescriptor">错误描述符</param>
    public static void SafeExecute(
        SourceProductionContext context,
        string className,
        Action action,
        DiagnosticDescriptor failureDescriptor = null,
        DiagnosticDescriptor errorDescriptor = null)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            ReportError(context, errorDescriptor ?? DiagnosticDescriptors.BoGenerationError, className, ex);
        }
    }

    /// <summary>
    /// 报告生成失败
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="descriptor">诊断描述符</param>
    /// <param name="className">类名</param>
    public static void ReportFailure(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        string className)
    {
        if (descriptor != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, className));
        }
    }

    /// <summary>
    /// 报告生成错误
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="descriptor">诊断描述符</param>
    /// <param name="className">类名</param>
    /// <param name="exception">异常信息</param>
    public static void ReportError(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        string className,
        Exception exception)
    {
        if (descriptor != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, className, exception?.Message));
        }
    }

    /// <summary>
    /// 验证生成结果
    /// </summary>
    /// <param name="context">源码生成上下文</param>
    /// <param name="generatedClass">生成的类</param>
    /// <param name="className">类名</param>
    /// <param name="failureDescriptor">失败描述符</param>
    /// <returns>验证是否通过</returns>
    public static bool ValidateGenerationResult(
        SourceProductionContext context,
        object generatedClass,
        string className,
        DiagnosticDescriptor failureDescriptor)
    {
        if (generatedClass == null)
        {
            ReportFailure(context, failureDescriptor, className);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 创建安全的属性生成委托
    /// </summary>
    /// <typeparam name="T">成员类型</typeparam>
    /// <param name="generator">生成器</param>
    /// <param name="propertyGenerator">属性生成器</param>
    /// <returns>安全的属性生成委托</returns>
    public static Func<T, PropertyDeclarationSyntax?> CreateSafePropertyGenerator<T>(
        TransitiveDtoGenerator generator,
        Func<T, PropertyDeclarationSyntax?> propertyGenerator) where T : MemberDeclarationSyntax
    {
        return member =>
        {
            try
            {
                return propertyGenerator?.Invoke(member);
            }
            catch (Exception)
            {
                // 单个属性生成失败不影响其他属性
                return null;
            }
        };
    }
}