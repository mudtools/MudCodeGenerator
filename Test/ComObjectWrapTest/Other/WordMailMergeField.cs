using Mud.Common.CodeGenerator.Extensions;
using System.Runtime.InteropServices;

namespace ComObjectWrapTest.Other;

internal class WordMailMergeField : IWordMailMergeField
{
    private MsWord.MailMergeField? _mailMergeField;
    private bool _disposedValue;

    internal WordMailMergeField(MsWord.MailMergeField mailMergeField)
    {
        _mailMergeField = mailMergeField ?? throw new ArgumentNullException(nameof(mailMergeField));
        _disposedValue = false;
    }

    public IWordApplication? Application => _mailMergeField != null ? new WordApplication(_mailMergeField.Application) : null;

    public object? Parent => _mailMergeField?.Parent;

    public bool locked
    {
        get => _mailMergeField?.Locked ?? false;
        set
        {
            if (_mailMergeField != null)
                _mailMergeField.Locked = value;
        }
    }

    public WdFieldType Type => _mailMergeField?.Type.EnumConvert(WdFieldType.wdFieldEmpty) ?? WdFieldType.wdFieldEmpty;


    public WdFieldType Test
    {
        get => _mailMergeField?.Test.EnumConvert(WdFieldType.wdFieldEmpty) ?? WdFieldType.wdFieldEmpty;
        set
        {
            if (_mailMergeField != null)
                _mailMergeField.Test = value.EnumConvert(MsWord.WdFieldType.wdFieldEmpty);
        }
    }


    public IWordRange? Code => _mailMergeField?.Code != null ? new WordRange(_mailMergeField.Code) : null;

    #region 方法实现
    public void Delete()
    {
        try
        {
            _mailMergeField?.Delete();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("执行MailMergeField对象的Delete方法失败。", ex);
        }
    }
    public void Delete(int index)
    {
        if (_mailMergeField == null)
            throw new ObjectDisposedException(nameof(_mailMergeField));

        try
        {
            _mailMergeField.Delete(index);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("执行MailMergeField对象的Delete方法失败。", ex);
        }
    }


    public IWordRange Copy(int index)
    {
        if (_mailMergeField == null)
            throw new ObjectDisposedException(nameof(_mailMergeField));

        try
        {
            var comObj = _mailMergeField.Copy(index);
            if (comObj == null)
                return null;
            return new WordRange(comObj);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("执行MailMergeField对象的Copy方法失败。", ex);
        }

    }
    #endregion

    #region IDisposable 实现
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;

        if (disposing && _mailMergeField != null)
        {
            Marshal.ReleaseComObject(_mailMergeField);
            _mailMergeField = null;
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
