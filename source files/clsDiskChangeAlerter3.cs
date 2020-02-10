using System;
using System.Management;
using System.Collections.Generic;
using System.Diagnostics;

namespace FileRenamer3
{
	public enum DriveChangeType { Create, Remove, MediaChange, Other };

	public class DriveChangedArgs
	{
		public string DriveLetter = "";
		public string InterfaceType = "";
		public DriveChangeType ChangeType = DriveChangeType.Other;
	}
	public delegate void EventChangedAlertHandler(DriveChangedArgs e);


	public class clsDiskChangeAlerter3 : IDisposable
	{
		ManagementEventWatcher Watcher = new ManagementEventWatcher();
		ManagementEventWatcher Watcher2 = new ManagementEventWatcher();
		//ManagementEventWatcher Watcher3 = new ManagementEventWatcher();
		public event EventChangedAlertHandler DiskChangeEvent;

		/*
		private Dictionary<string, string> m_USBdic = new Dictionary<string, string>();
		public Dictionary<string, string> USBdic => m_USBdic;
		*/

		private List<string> m_UsbDriveList = new List<string>();
		public List<string> UsbDriveList => m_UsbDriveList;

		public void IsExiting()
		{
			Watcher.Stop();
			Watcher.Dispose();
			Watcher = null;
		}

		public clsDiskChangeAlerter3()
		{
			WqlEventQuery q1 = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 "
				+ "WHERE TargetInstance ISA 'Win32_DiskDrive' OR TargetInstance ISA 'Win32_LogicalDisk'");
			Watcher.Query = q1;
			Watcher.EventArrived += new EventArrivedEventHandler(Watcher_EventArrived);
			Watcher.Start();

			WqlEventQuery q2 = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 "
				+ "WHERE TargetInstance ISA 'Win32_DiskDrive' OR TargetInstance ISA 'Win32_LogicalDisk'");
			Watcher2.Query = q2;
			Watcher2.EventArrived += new EventArrivedEventHandler(Watcher_EventArrived);
			Watcher2.Start();
			/*
			WqlEventQuery q3 = new WqlEventQuery("SELECT * FROM __InstanceModificationEvent WITHIN 2 "
				+ "WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			Watcher3.Query = q3;
			Watcher3.EventArrived += new EventArrivedEventHandler(Watcher_EventArrived);
			Watcher3.Start();
			*/
		}

		/*
		0 = Unknown
		1 = No Root Directory
		2 = Removable Disk
		3 = Local Disk
		4 = Network Drive
		5 = Compact Disc
		6 = RAM Disk
		*/

		private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
		{
			DriveChangedArgs a1 = new DriveChangedArgs();
			/*
			int iDriveType = 0;
			ManagementBaseObject mbo, obj;
			mbo = (ManagementBaseObject)e.NewEvent;
			obj = (ManagementBaseObject)mbo["TargetInstance"];
			try
			{ iDriveType = int.Parse(obj["DriveType"].ToString()); }
			catch
			{ iDriveType = 0; }
			DriveChangedArgs a1 = new DriveChangedArgs
			{
				DriveLetter = obj["Name"].ToString()
			};
			if (obj.ClassPath.ToString().ToLower().Contains("mapped") || iDriveType == 4)
				a1.InterfaceType = "MappedDrive";
			if (obj.ClassPath.ToString().Contains("Win32_DiskDrive") || iDriveType == 4)
			{
				try
				{
					a1.InterfaceType = obj["InterfaceType"].ToString();
				}
				catch
				{
					a1.InterfaceType = "unknown";
				}
				try
				{
					a1.DriveLetter = GetDriveLetterFromDisk(obj["Name"].ToString());
					if (!USBdic.ContainsKey(obj["Name"].ToString()))
					{ USBdic.Add(obj["Name"].ToString(), a1.DriveLetter); }
				}
				catch
				{
					if (USBdic.ContainsKey(obj["Name"].ToString()))
					{
						a1.DriveLetter = USBdic[obj["Name"].ToString()];
						USBdic.Remove(obj["Name"].ToString());
					}
					else
						a1.DriveLetter = "unknown";
				}
			}
			switch (mbo.ClassPath.ClassName)
			{
				case "__InstanceCreationEvent":
					a1.ChangeType = DriveChangeType.Create;
					break;
				case "__InstanceDeletionEvent":
					a1.ChangeType = DriveChangeType.Remove;
					break;
				case "__InstanceModificationEvent":
					a1.ChangeType = DriveChangeType.MediaChange;
					break;
				default:
					a1.ChangeType = DriveChangeType.Other;
					break;
			}
			*/

			try
			{
				DiskChangeEvent.Invoke(a1);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Watchereventarrived(): " + ex);
			}
		}

		/*

		private string GetDriveLetterFromDisk(string Name)
		{
			ObjectQuery oq_part, oq_disk;
			ManagementObjectSearcher mos_part, mos_disk;
			string ans = "";

			// WMI queries use the "\" as an escape charcter
			//    Name = Name.Replace(@"\", @"\\");

			//' First we map the Win32_DiskDrive instance with the association called
			//' Win32_DiskDriveToDiskPartition. Then we map the Win32_DiskPartion
			//' instance with the assocation called Win32_LogicalDiskToPartition

			oq_part = new ObjectQuery("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + Name + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
			mos_part = new ManagementObjectSearcher(oq_part);
			foreach (ManagementObject obj_part in mos_part.Get())
			{
				oq_disk = new ObjectQuery("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + obj_part["DeviceID"] + "'} WHERE AssocClass = Win32_LogicalDiskToPartition");
				mos_disk = new ManagementObjectSearcher(oq_disk);
				foreach (ManagementObject obj_disk in mos_disk.Get())
				{
					ans += obj_disk["Name"] + ",";
				}
			}
			return ans = ans.Substring(0, ans.Length - 1);
		}

		*/

		public void GetUSBdrives()
		{
			m_UsbDriveList.Clear();

			WqlObjectQuery qDrives = new WqlObjectQuery("select * from Win32_DiskDrive where InterfaceType='USB'");
			ManagementObjectSearcher oSearch = new ManagementObjectSearcher(qDrives);
			ManagementObjectCollection cDrives = oSearch.Get();
			foreach (ManagementObject obj in cDrives)
			{
				Debug.WriteLine("drive win32 name" + obj["Name"]);
				// associate physical disks with partitions
				ManagementObjectSearcher partsearch = new ManagementObjectSearcher(
					 "associators of {Win32_DiskDrive.DeviceID='" + obj["DeviceID"] + "'} " +
					 "where AssocClass = Win32_DiskDriveToDiskPartition");
				ManagementObjectCollection partcol = partsearch.Get();
				foreach (ManagementObject partobj in partcol)
				{
					if (partobj != null)
					{
						Debug.WriteLine("partition name " + partobj["Name"]);
						ManagementObjectSearcher lsearcher = new ManagementObjectSearcher(
							 "associators of {Win32_DiskPartition.DeviceID='" + partobj["DeviceID"] + "'} " +
							 "where AssocClass= Win32_LogicalDiskToPartition");
						ManagementObjectCollection lcol = lsearcher.Get();
						foreach (ManagementObject logical in lcol)
						{
							if (logical != null)
							{
								Debug.WriteLine("logical=" + logical["Name"]);
								m_UsbDriveList.Add(logical["Name"].ToString() + @"\");
							}
							else
								Debug.WriteLine("logical is null");
						}
					}
					else
						Debug.WriteLine("partition is null");
				}
			}
			cDrives.Dispose();
			cDrives = null;
			qDrives = null;
			oSearch.Dispose();
			oSearch = null;
		} // GetUSBdrives()

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// free managed resources
				if (Watcher != null)
					Watcher.Dispose();
			}
			// free native resources if there are any.
		}

	}
}

