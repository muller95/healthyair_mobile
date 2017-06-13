using Android.App;
using Android.Bluetooth;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;
using System.Collections.Generic;
using Android.Views;

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

	public class DiscoveredBluetoothDevice
	{
		public DiscoveredBluetoothDevice(string name, string address)
		{
			Name = name;
			Address = address;
		}

		public string Name { get; set; }
		public string Address { get; set; }
	}

	public class BluetoothListAdapter : BaseAdapter<DiscoveredBluetoothDevice>
	{
		private Activity activity;
		private List<DiscoveredBluetoothDevice> devices;

		public BluetoothListAdapter(Activity activity, List<DiscoveredBluetoothDevice> devices)
			:base()
		{
			this.activity = activity;
			this.devices = devices;
		}

		public override DiscoveredBluetoothDevice this[int position] => devices[position];

		public override int Count => devices.Count;

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView;

			if (view == null)
				view = activity.LayoutInflater.Inflate(Resource.Layout.BluetoothListViewRow, parent, false);

			DiscoveredBluetoothDevice device = this[position];
			view.FindViewById<TextView>(Resource.Id.Name).Text = device.Name;
			view.FindViewById<TextView>(Resource.Id.Address).Text = device.Address;

			return view;
		}

		public void Add(DiscoveredBluetoothDevice device)
		{
			devices.Add(device);
		}
	}

	[Activity(Label = "Healthy air", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		const int RequestEnableBt = 1;
		BluetoothAdapter adapter;
		BluetoothReceiver receiver;
		Dictionary<string, BluetoothDevice> devices;
		ListView bluetoothListView;
		BluetoothListAdapter bluetoothListAdapter;

		protected enum MessageType
		{
			Error,
			Warning
		}

		private void OnBluetoothDeviceDiscovered(object sender, BluetoothDeviceDiscoveredEventArgs args)
		{
			devices.Add(args.Device.Address, args.Device);
			bluetoothListAdapter.Add(new DiscoveredBluetoothDevice(args.Device.Name, args.Device.Address));
			bluetoothListView.Adapter = bluetoothListAdapter;
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

			devices = new Dictionary<string, BluetoothDevice>();
			
			SetContentView(Resource.Layout.Main);
		}

		protected override void OnResume()
		{
			base.OnResume();

			bluetoothListView = FindViewById<ListView>(Resource.Id.bluetoothListView);
			bluetoothListAdapter = new BluetoothListAdapter(this, new List<DiscoveredBluetoothDevice>());
			bluetoothListView.Adapter = bluetoothListAdapter;

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

