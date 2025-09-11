namespace RSLBot.Core.Services;

using System;
using System.Runtime.InteropServices;

// internal static class Direct3D11Interop
// {
//     [DllImport("d3d11.dll", SetLastError = true, PreserveSig = true)]
//     private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevicePtr, out IntPtr graphicsDevice);
//
//     public static IDirect3DDevice CreateDirect3DDeviceFromVorticeDevice(ID3D11Device vorticeDevice)
//     {
//         using var dxgiDevice = vorticeDevice.QueryInterface<IDXGIDevice>();
//         int result = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr iinspectable);
//         if (result < 0)
//         {
//             throw new Exception($"Не вдалося створити WinRT-пристрій з Vortice-пристрою. HRESULT: {result:X}");
//         }
//
//         try
//         {
//             return MarshalInterface<IDirect3DDevice>.FromAbi(iinspectable);
//         }
//         finally
//         {
//             Marshal.Release(iinspectable);
//         }
//     }
// }