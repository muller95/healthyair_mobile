using Android.App;
using Android.Bluetooth;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;
using System.Collections.Generic;

namespace Healthy_air
{
	public class BluetoothDeviceDiscoveredEventArgs : EventArgs
	{
		private BluetoothDevice device;

		public BluetoothDeviceDiscoveredEventArgs(BluetoothDevice device)
		{
			this.device = device;
		}

		public BluetoothDevice Device
		{
			get { return device; }
		}
	}

	public class BluetoothReceiver : BroadcastReceiver
	{
		public delegate void BluetoothDeviceDiscoveredEventHandler(object sender, BluetoothDeviceDiscoveredEventArgs args);
		public event BluetoothDeviceDiscoveredEventHandler BluetoothDeviceDiscovered;
		public override void OnReceive(Context context, Intent intent)
		{
			if (intent.Action == BluetoothDevice.ActionFound)
			{
				BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
				BluetoothDeviceDiscovered(this, new BluetoothDeviceDiscoveredEventArgs(device));
			}
		}
	}

	[Activity(Label = "Healthy air", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		const int RequestEnableBt = 1;
		BluetoothAdapter adapter;
		BluetoothReceiver receiver;
		Dictionary<string, BluetoothDevice> devices;

		protected enum MessageType
		{
			Error,
			Warning
		}

		private void OnBluetoothDeviceDiscovered(object sender, BluetoothDeviceDiscoveredEventArgs args)
		{
			devices.Add(args.Device.Address, args.Device);	
		}

		protected void ShowMessage(string message, MessageType type)
		{
			string title = GetString(type == MessageType.Error ? Resource.String.Error : Resource.String.Warning);

			AlertDialog.Builder builder = new AlertDialog.Builder(this);
			builder.SetMessage(message);
			builder.SetTitle(title);
			builder.SetMessage(message);
			builder.SetPositiveButton("OK", (sender, args) => { });
			builder.Create().Show();

		}

		protected void ListBluetoothDevices()
		{

		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			if (resultCode == Result.Canceled)
				ShowMessage(GetString(Resource.String.BluetoothOff), MessageType.Warning);
			else
				adapter = null;
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			adapter = BluetoothAdapter.DefaultAdapter;

			if (adapter == null)
			{
				ShowMessage(GetString(Resource.String.BluetoothNotSupported), MessageType.Error);
				System.Environment.Exit(0);
			}


			if (!adapter.IsEnabled)
			{
				Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult(enableBtIntent, RequestEnableBt);
			}

			

			SetContentView(Resource.Layout.Main);
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (adapter != null)
			{
				if (adapter.IsDiscovering)
				{
					adapter.CancelDiscovery();
				}
				bool started = adapter.StartDiscovery();
				receiver = new BluetoothReceiver();
				receiver.BluetoothDeviceDiscovered += OnBluetoothDeviceDiscovered;
				IntentFilter filter = new IntentFilter();
				filter.AddAction(BluetoothDevice.ActionFound);
				filter.AddAction(BluetoothAdapter.ActionDiscoveryStarted);
				filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
				this.RegisterReceiver(receiver, filter);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			if (adapter != null)
			{
				if (adapter.IsDiscovering)
					adapter.CancelDiscovery();
				this.UnregisterReceiver(receiver);
			}
		}
	}
}

