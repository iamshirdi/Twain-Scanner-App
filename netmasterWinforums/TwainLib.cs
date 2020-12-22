using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace TwainLib
{
public enum TwainCommand
	{
	Not				= -1,
	Null			= 0,
	TransferReady	= 1,
	CloseRequest	= 2,
	CloseOk			= 3,
	DeviceEvent		= 4
	}




public class Twain
	{
	private const short CountryUSA		= 1;
	private const short LanguageUSA		= 13;

	public Twain()
		{
		appid = new TwIdentity();
		appid.Id				= IntPtr.Zero;
		appid.Version.MajorNum	= 1;
		appid.Version.MinorNum	= 1;
		appid.Version.Language	= LanguageUSA;
		appid.Version.Country	= CountryUSA;
		appid.Version.Info		= "Hack 1";
		appid.ProtocolMajor		= TwProtocol.Major;
		appid.ProtocolMinor		= TwProtocol.Minor;
		appid.SupportedGroups	= (int)(TwDG.Image | TwDG.Control);
		appid.Manufacturer		= "NETMaster";
		appid.ProductFamily		= "Freeware";
		appid.ProductName		= "Hack";

		srcds = new TwIdentity();
		srcds.Id = IntPtr.Zero;

		evtmsg.EventPtr = Marshal.AllocHGlobal( Marshal.SizeOf( winmsg ) );
		}

	~Twain()
		{
		Marshal.FreeHGlobal( evtmsg.EventPtr );
		}




	public void Init( IntPtr hwndp )
		{
		Finish();
		TwRC rc = DSMparent( appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.OpenDSM, ref hwndp );
		if( rc == TwRC.Success )
			{
			rc = DSMident( appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.GetDefault, srcds );
			if( rc == TwRC.Success )
				hwnd = hwndp;
			else
				rc = DSMparent( appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref hwndp );
			}
		}

	public void Select()
		{
		TwRC rc;
		CloseSrc();
		if( appid.Id == IntPtr.Zero )
			{
			Init( hwnd );
			if( appid.Id == IntPtr.Zero )
				return;
			}
		rc = DSMident( appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.UserSelect, srcds );
		}


	public void Acquire()
		{
		TwRC rc;
		CloseSrc();
		if( appid.Id == IntPtr.Zero )
			{
			Init( hwnd );
			if( appid.Id == IntPtr.Zero )
				return;
			}
		rc = DSMident( appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.OpenDS, srcds );
		if( rc != TwRC.Success )
			return;

		TwCapability cap = new TwCapability( TwCap.XferCount, 1 );
		rc = DScap( appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Set, cap );
		if( rc != TwRC.Success )
			{
			CloseSrc();
			return;
			}

		TwUserInterface	guif = new TwUserInterface();
		guif.ShowUI = 1;
		guif.ModalUI = 1;
		guif.ParentHand = hwnd;
		rc = DSuserif( appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDS, guif );
		if( rc != TwRC.Success )
			{
			CloseSrc();
			return;
			}
		}


	public ArrayList TransferPictures()
		{
		ArrayList pics = new ArrayList();
		if( srcds.Id == IntPtr.Zero )
			return pics;

		TwRC rc;
		IntPtr hbitmap = IntPtr.Zero;
		TwPendingXfers pxfr = new TwPendingXfers();
		
		do
			{
			pxfr.Count = 0;
			hbitmap = IntPtr.Zero;

			TwImageInfo	iinf = new TwImageInfo();
			rc = DSiinf( appid, srcds, TwDG.Image, TwDAT.ImageInfo, TwMSG.Get, iinf );
			if( rc != TwRC.Success )
				{
				CloseSrc();
				return pics;
				}

			rc = DSixfer( appid, srcds, TwDG.Image, TwDAT.ImageNativeXfer, TwMSG.Get, ref hbitmap );
			if( rc != TwRC.XferDone )
				{
				CloseSrc();
				return pics;
				}

			rc = DSpxfer( appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, pxfr );
			if( rc != TwRC.Success )
				{
				CloseSrc();
				return pics;
				}

			pics.Add( hbitmap );
			}
		while( pxfr.Count != 0 );

		rc = DSpxfer( appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, pxfr );
		return pics;
		}


	public TwainCommand PassMessage( ref Message m )
		{
		if( srcds.Id == IntPtr.Zero )
			return TwainCommand.Not;

		int pos = GetMessagePos();

		winmsg.hwnd		= m.HWnd;
		winmsg.message	= m.Msg;
		winmsg.wParam	= m.WParam;
		winmsg.lParam	= m.LParam;
		winmsg.time		= GetMessageTime();
		winmsg.x		= (short) pos;
		winmsg.y		= (short) (pos >> 16);
		
		Marshal.StructureToPtr( winmsg, evtmsg.EventPtr, false );
		evtmsg.Message = 0;
		TwRC rc = DSevent( appid, srcds, TwDG.Control, TwDAT.Event, TwMSG.ProcessEvent, ref evtmsg );
		if( rc == TwRC.NotDSEvent )
			return TwainCommand.Not;
		if( evtmsg.Message == (short) TwMSG.XFerReady )
			return TwainCommand.TransferReady;
		if( evtmsg.Message == (short) TwMSG.CloseDSReq )
			return TwainCommand.CloseRequest;
		if( evtmsg.Message == (short) TwMSG.CloseDSOK )
			return TwainCommand.CloseOk;
		if( evtmsg.Message == (short) TwMSG.DeviceEvent )
			return TwainCommand.DeviceEvent;

		return TwainCommand.Null;
		}

	public void CloseSrc()
		{
		TwRC rc;
		if( srcds.Id != IntPtr.Zero )
			{
			TwUserInterface	guif = new TwUserInterface();
			rc = DSuserif( appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.DisableDS, guif );
				if (rc != TwRC.Failure)
				{
					rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.CloseDS, srcds);
				}
			}
		}

	public void Finish()
		{
		TwRC rc;
		CloseSrc();
		if( appid.Id != IntPtr.Zero )
			rc = DSMparent( appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref hwnd );
		appid.Id = IntPtr.Zero;
		}

	private IntPtr		hwnd;
	private TwIdentity	appid;
	private TwIdentity	srcds;
	private TwEvent		evtmsg;
	private WINMSG		winmsg;
	


	// ------ DSM entry point DAT_ variants:
		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSMparent( [In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSMident( [In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwIdentity idds );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSMstatus( [In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat );


	// ------ DSM entry point DAT_ variants to DS:
		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSuserif( [In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwUserInterface guif );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSevent( [In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref TwEvent evt );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSstatus( [In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DScap( [In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwCapability capa );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSiinf( [In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwImageInfo imginf );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSixfer( [In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap );

		[DllImport("twain_32.dll", EntryPoint="#1")]
	private static extern TwRC DSpxfer( [In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwPendingXfers pxfr );


		[DllImport("kernel32.dll", ExactSpelling=true)]
	internal static extern IntPtr GlobalAlloc( int flags, int size );
		[DllImport("kernel32.dll", ExactSpelling=true)]
	internal static extern IntPtr GlobalLock( IntPtr handle );
		[DllImport("kernel32.dll", ExactSpelling=true)]
	internal static extern bool GlobalUnlock( IntPtr handle );
		[DllImport("kernel32.dll", ExactSpelling=true)]
	internal static extern IntPtr GlobalFree( IntPtr handle );

		[DllImport("user32.dll", ExactSpelling=true)]
	private static extern int GetMessagePos();
		[DllImport("user32.dll", ExactSpelling=true)]
	private static extern int GetMessageTime();


		[DllImport("gdi32.dll", ExactSpelling=true)]
	private static extern int GetDeviceCaps( IntPtr hDC, int nIndex );

		[DllImport("gdi32.dll", CharSet=CharSet.Auto)]
	private static extern IntPtr CreateDC( string szdriver, string szdevice, string szoutput, IntPtr devmode );

		[DllImport("gdi32.dll", ExactSpelling=true)]
	private static extern bool DeleteDC( IntPtr hdc );


		//bit map

		// GDI External method needed Place it within your class
		[DllImport("GdiPlus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipCreateBitmapFromGdiDib(IntPtr pBIH,
		 IntPtr pPix, out IntPtr pBitmap);



		//bitmap function

		public static Bitmap BitmapFromDIB(IntPtr pDIB)
		{
			// get pointer to bitmap header info       
			IntPtr pPix = GetPixelInfo(pDIB);

			// Call external GDI method 
			MethodInfo my = typeof(Bitmap).GetMethod("FromGDIplus", BindingFlags.Static | BindingFlags.NonPublic);
			if (my == null)
				return null;

			// Initialize memory pointer where Bitmap will be saved 
			IntPtr pBmp = IntPtr.Zero;

			// Call external methosd that saves bitmap into pointer 
			int status = GdipCreateBitmapFromGdiDib(pDIB, pPix, out pBmp);

			// If success return bitmap, if failed return null 
			if ((status == 0) && (pBmp != IntPtr.Zero))
				return (Bitmap)my.Invoke(null, new object[] { pBmp });
			else
				return null;
		}

		// THIS METHOD GETS THE POINTER TO THE BITMAP HEADER INFO 
		public static IntPtr GetPixelInfo(IntPtr bmpPtr)
		{
			BITMAPINFOHEADER bmi = (BITMAPINFOHEADER)Marshal.PtrToStructure(bmpPtr, typeof(BITMAPINFOHEADER));

			if (bmi.biSizeImage == 0)
				bmi.biSizeImage = (uint)(((((bmi.biWidth * bmi.biBitCount) + 31) & ~31) >> 3) * bmi.biHeight);

			int p = (int)bmi.biClrUsed;
			if ((p == 0) && (bmi.biBitCount <= 8))
				p = 1 << bmi.biBitCount;
			p = (p * 4) + (int)bmi.biSize + (int)bmpPtr;
			return (IntPtr)p;
		}

		// convert and save to tiff
		public void SavehDibToTiff ( int hDIB, string fileName, int xRes, int yRes) 
{ 
  // Identify the memory pointer to the DIB Handler (hDIB) 
  IntPtr dibPtr = new IntPtr (hDIB); 

  // Save the contents of DIB pointer into bitmap object 
  Bitmap myBitmap = BitmapFromDIB (dibPtr); 

  // Set resolution if needed 
  if (xRes> 0 && yRes> 0) 
      myBitmap.SetResolution (xRes, yRes); 

  // Create an instance of the windows TIFF encoder 
  ImageCodecInfo ici = GetEncoderInfo ( "image / tiff" ); 

  // Define encoder parameters
  EncoderParameters eps = new EncoderParameters (1); // only one parameter in this case (compression) 

  // Create an Encoder Value for TIFF compression Group 4 
  long ev = ( long ) EncoderValue .CompressionCCITT4; 
  eps.Param [0] = new EncoderParameter (System.Drawing.Imaging. Encoder .Compression, ev); 

  // Save file                      
   myBitmap.Save (fileName, ici, eps); 
}
// Helper to get Encoder from Windows for file type. 
public static ImageCodecInfo GetEncoderInfo ( String mimeType) 
{ 
  ImageCodecInfo [] encoders = ImageCodecInfo .GetImageEncoders (); 
  for ( int j = 0; j <encoders.Length; ++ j) 
  { 
      if (encoders [j] .MimeType == mimeType) 
          return encoders [j]; 
  } 
  return null ; 
}





		public static int ScreenBitDepth {
		get {
			IntPtr screenDC = CreateDC( "DISPLAY", null, null, IntPtr.Zero );
			int bitDepth = GetDeviceCaps( screenDC, 12 );
			bitDepth *= GetDeviceCaps( screenDC, 14 );
			DeleteDC( screenDC );
			return bitDepth;
			}
		}




		[StructLayout(LayoutKind.Sequential, Pack=4)]
	internal struct WINMSG
		{
		public IntPtr		hwnd;
		public int			message;
		public IntPtr		wParam;
		public IntPtr		lParam;
		public int			time;
		public int			x;
		public int			y;
		}


	} // class Twain
}
