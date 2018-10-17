using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Specialized
{
    public interface IStorage
    {
        IFirmware[] Firmware { get; }
        IStorageEntities<Recording> Recordings { get; }
    }

    public interface IStorageEntities<T>
    {
		event EventHandler<StorageEntityChangeEventArgs> Changed;
        EntityInfo[] GetAll();
        T Get(EntityInfo entityInfo);
        void Save(T entity, EntityInfo info);
        bool Move(EntityInfo source, EntityInfo destination);
        bool Copy(EntityInfo source, EntityInfo destination);
        bool Delete(EntityInfo entity);
    }

    public class EntityInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Guid ID { get; set; }
		public int SortOrder { get; set; }
    }

    public interface IFirmware
    {
        string Model { get; }
        Version Version { get; }
        DateTime ReleaseDate { get; }
        string GetHexFile();
        void DeleteHexFile();
    }

	public class StorageEntityChangeEventArgs : EventArgs
	{
		public StorageEntityChangeType Type { get; set; }
		public EntityInfo Source { get; set; }
		public EntityInfo Destination { get; set; }
	}

	public enum StorageEntityChangeType
	{
		Saved,
		Moved,
		Copied,
		Deleted
	}
	
}
