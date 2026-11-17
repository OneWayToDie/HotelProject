using System;
using System.Runtime.InteropServices;

namespace HotelApp.Services
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CRoom
	{
		public int Id;
		public int BuildingId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string Number;
		public int Capacity;
		public double BasePrice;
		public int Status; // 0-Free, 1-Occupied, 2-Cleaning
		public int Type;   // 0-Standard, 1-Lux, 2-Conference
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string GuestName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] public string GuestPassport;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string CheckInDate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string CheckOutDate;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CBuilding
	{
		public int Id;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string Name;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CGuest
	{
		public int Id;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string FullName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] public string Passport;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CBooking
	{
		public int Id;
		public int RoomId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string GuestName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] public string Passport;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string CheckInDate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string CheckOutDate;
	}

	internal static class NativeMethods
	{
		private const string DllName = "HotelLogic.dll";

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr Hotel_Create();

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Hotel_Destroy(IntPtr ptr);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Hotel_AddBuilding(IntPtr ptr, string name);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Hotel_AddRoom(IntPtr ptr, int buildingId, int type, string number, double price);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool Hotel_CheckIn(IntPtr ptr, int roomId, string name, string passport, string inDate, string outDate, int days);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double Hotel_CheckOut(IntPtr ptr, int roomId);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Hotel_FinishCleaning(IntPtr ptr, int roomId);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void Hotel_UpdatePrice(IntPtr ptr, int roomId, double newPrice);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool Hotel_Save(IntPtr ptr, string filename);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool Hotel_Load(IntPtr ptr, string filename);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Hotel_GetAllRooms(IntPtr ptr, [Out] CRoom[] buffer, int maxSize);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Hotel_GetAllBuildings(IntPtr ptr, [Out] CBuilding[] buffer, int maxSize);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Hotel_GetAllGuests(IntPtr ptr, [Out] CGuest[] buffer, int maxSize);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern double Hotel_GetTotalRevenue(IntPtr ptr);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool Hotel_BookRoom(IntPtr ptr, int roomId, string name, string passport, string inDate, string outDate);

		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int Hotel_GetAllBookings(IntPtr ptr, [Out] CBooking[] buffer, int maxSize);
	}
}