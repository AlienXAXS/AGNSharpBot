using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLite.Net.Attributes;
using AutoIncrementAttribute = SQLite.Net.Attributes.AutoIncrementAttribute;
using IndexedAttribute = SQLite.Net.Attributes.IndexedAttribute;
using PrimaryKeyAttribute = SQLite.Net.Attributes.PrimaryKeyAttribute;

namespace GameWatcher.DB.Tables
{
    class GameMemory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string Name { get; set; }
    }
}
