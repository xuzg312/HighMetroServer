using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace HighMetroServer.HikVision;

public sealed class FrameData : IDisposable
{
    private IMemoryOwner<byte>? _bufferOwner;
    private GCHandle _pinnedHandle;
    private IntPtr _dataPtr;
    private bool _disposed;

    public FrameData(IMemoryOwner<byte> owner)
    {
        _bufferOwner = owner ?? throw new ArgumentNullException(nameof(owner));
        try
        {
            // 修复：显式指定泛型类型参数 <byte>
            if (MemoryMarshal.TryGetArray<byte>(owner.Memory, out var segment) && segment.Array != null)
            {
                _pinnedHandle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
                _dataPtr = _pinnedHandle.AddrOfPinnedObject() + segment.Offset;
            }
            else
            {
                var array = owner.Memory.ToArray();
                _pinnedHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                _dataPtr = _pinnedHandle.AddrOfPinnedObject();
            }
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }
    public int DataSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int YSize { get; set; }
    public int UvSize { get; set; }
    public int FrameNumber { get; set; }

    public unsafe byte* GetYPtr()
    {
        if (_disposed || _dataPtr == IntPtr.Zero) return null;
        return (byte*)_dataPtr;
    }

    public unsafe byte* GetVPtr()
    {
        if (_disposed || _dataPtr == IntPtr.Zero) return null;
        return (byte*)_dataPtr + YSize;
    }

    public unsafe byte* GetUPtr()
    {
        if (_disposed || _dataPtr == IntPtr.Zero) return null;
        return (byte*)_dataPtr + YSize + UvSize;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_pinnedHandle.IsAllocated)
        {
            _pinnedHandle.Free();
        }
        _bufferOwner?.Dispose();
        _bufferOwner = null;
        _dataPtr = IntPtr.Zero;
    }
}