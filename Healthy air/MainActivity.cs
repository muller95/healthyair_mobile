using Android.App;
using Android.Bluetooth;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Views;

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
			: base()
		{
			this.activity = activity;
			this.devices = devices;
			NamePrefix = "";
			AddressPrefix = "";
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
			view.FindViewById<TextView>(Resource.Id.Name).Text = String.Format("{0} {1}", NamePrefix, device.Name);
			view.FindViewById<TextView>(Resource.Id.Address).Text = String.Format("{0} {1}", AddressPrefix, device.Address);

			return view;
		}

		public void Add(DiscoveredBluetoothDevice device)
		{
			devices.Add(device);
		}

		public void Clear()
		{
			devices.Clear();
		}

		public string NamePrefix { get; set; }
		public string AddressPrefix { get; set; }
	}

	[Activity(Label = "Healthy air", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		const int RequestEnableBt = 1;
		BluetoothAdapter adapter;
		BluetoothReceiver receiver;
		ListView bluetoothListView;
		BluetoothListAdapter bluetoothListAdapter;

		protected enum MessageType
		{
			Error,
			Warning
		}

		private void OnBluetoothDeviceDiscovered(object sender, BluetoothDeviceDiscoveredEventArgs args)
		{
			bluetoothListAdapter.Add(new DiscoveredBluetoothDevice(args.Device.Name, args.Device.Address));
			bluetoothListView.Adapter = bluetoothListAdapter;
		}

		private void EnableBluetooth()
		{
			Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
			StartActivityForResult(enableBtIntent, RequestEnableBt);
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
				EnableBluetooth();

			SetContentView(Resource.Layout.Main);

			FindViewById<Button>(Resource.Id.buttonUpdate).Click += ButtonUpdateOnClick;
		}

		protected override void OnResume()
		{
			base.OnResume();

			bluetoothListView = FindViewById<ListView>(Resource.Id.bluetoothListView);
			bluetoothListAdapter = new BluetoothListAdapter(this, new List<DiscoveredBluetoothDevice>());
			bluetoothListAdapter.NamePrefix = GetString(Resource.String.BluetoothName);
			bluetoothListAdapter.AddressPrefix = GetString(Resource.String.BluetoothAddress);
			bluetoothListView.Adapter = bluetoothListAdapter;

			if (!adapter.IsEnabled)
				return;

			if (adapter.IsDiscovering)
			{
				adapter.CancelDiscovery();
			}
			bool started = adapter.StartDiscovery();
			receiver = new BluetoothReceiver();
			receiver.BluetoothDeviceDiscovered += OnBluetoothDeviceDiscovered;
			IntentFilter filter = new IntentFilter();
			filter.AddAction(BluetoothDevice.ActionFound);
			this.RegisterReceiver(receiver, filter);

		}

		protected override void OnPause()
		{
			base.OnPause();
			if (!adapter.IsEnabled)
				return;

			if (adapter.IsDiscovering)
				adapter.CancelDiscovery();
			this.UnregisterReceiver(receiver);

		}

		public void ButtonUpdateOnClick(object sender, EventArgs args)
		{
			if (!adapter.IsEnabled)
				EnableBluetooth();

			if (!adapter.IsEnabled)
				return;

			if (adapter.IsDiscovering)
				adapter.CancelDiscovery();

			bluetoothListAdapter.Clear();
			bluetoothListView.Adapter = bluetoothListAdapter;
			adapter.StartDiscovery();
		}
	}
}

