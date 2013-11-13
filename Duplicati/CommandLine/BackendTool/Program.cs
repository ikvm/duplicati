#region Disclaimer / License
// Copyright (C) 2011, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using Duplicati.Library.Interface;

namespace Duplicati.CommandLine.BackendTool
{
    class Program
    {
        static int Main(string[] _args)
        {
            try
            {
                List<string> args = new List<string>(_args);
                Dictionary<string, string> options = Library.Utility.CommandLineParser.ExtractOptions(args);

                if (!options.ContainsKey("auth_password") && !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("AUTH_PASSWORD")))
                    options["auth_password"] = System.Environment.GetEnvironmentVariable("AUTH_PASSWORD");

                if (!options.ContainsKey("auth_username") && !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("AUTH_USERNAME")))
                    options["auth_username"] = System.Environment.GetEnvironmentVariable("AUTH_USERNAME");

                if (options.ContainsKey("tempdir") && !string.IsNullOrEmpty(options["tempdir"]))
                    Library.Utility.TempFolder.SystemTempPath = options["tempdir"];

                
                string command = null;
                if (args.Count >= 2)
                {
                    if (args[0].Equals("list", StringComparison.InvariantCultureIgnoreCase))
                        command = "list";
                    else if (args[0].Equals("get", StringComparison.InvariantCultureIgnoreCase))
                        command = "get";
                    else if (args[0].Equals("put", StringComparison.InvariantCultureIgnoreCase))
                        command = "put";
                    else if (args[0].Equals("delete", StringComparison.InvariantCultureIgnoreCase))
                        command = "delete";
                    else if (args[0].Equals("create-folder", StringComparison.InvariantCultureIgnoreCase))
                        command = "create";
                }


                if (args.Count < 2 || args[0].ToLower() == "help" || args[0] == "?" || command == null)
                {
                    if (command == null && args.Count >= 2)
                    {
                        Console.WriteLine("Unsupported command: {0}", args[0]);
                        Console.WriteLine();
                    }   
                    
                    Console.WriteLine("Usage: <command> <protocol>://<username>:<password>@<path> [filename]");
                    Console.WriteLine("Example: LIST ftp://user:pass@server/folder");
                    Console.WriteLine();
                    Console.WriteLine("Supported backends: " + string.Join(",", Duplicati.Library.DynamicLoader.BackendLoader.Keys));
                    Console.WriteLine("Supported commands: GET PUT LIST DELETE CREATEFOLDER");

                    return 200;
                }
                
                using(var backend = Library.DynamicLoader.BackendLoader.GetBackend(args[1], options))
                {
                    if (backend == null)
                        throw new Exception("Backend not supported");
                        
                    if (command == "list")
                    {
                        if (args.Count != 2)
                            throw new Exception(string.Format("too many arguments: {0}", string.Join(",", args)));
                        Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", "Name", "Dir/File", "LastChange", "Size"));
                    
                        foreach(var e in backend.List())
                            Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", e.Name, e.IsFolder ? "Dir" : "File", e.LastModification, e.Size < 0 ? "" : Library.Utility.Utility.FormatSizeString(e.Size)));

                        return 0;
                    }
                    else if (command == "create")
                    {
                        if (args.Count != 2)
                            throw new Exception(string.Format("too many arguments: {0}", string.Join(",", args)));

                        backend.CreateFolder();
                        
                        return 0;
                    }
                    else if (command == "delete")
                    {
                        if (args.Count < 3)
                            throw new Exception("DELETE requires a filename argument");
                        if (args.Count > 3)
                            throw new Exception(string.Format("too many arguments: {0}", string.Join(",", args)));
                        backend.Delete(args[2]);
                        
                        return 0;
                    }
                    else if (command == "get")
                    {
                        if (args.Count < 3)
                            throw new Exception("GET requires a filename argument");
                        if (args.Count > 3)
                            throw new Exception(string.Format("too many arguments: {0}", string.Join(",", args)));
                        if (System.IO.File.Exists(args[2]))
                            throw new Exception("File already exists, not overwriting!");
                        backend.Get(args[2], args[2]);
                        
                        return 0;
                    }
                    else if (command == "put")
                    {
                        if (args.Count < 3)
                            throw new Exception("PUT requires a filename argument");
                        if (args.Count > 3)
                            throw new Exception(string.Format("too many arguments: {0}", string.Join(",", args)));
                           
                        backend.Put(args[2], args[2]);
                        
                        return 0;
                    }
                    
                    throw new Exception("Internal error");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Command failed: " + ex.Message);
                return 100;
            }
        }
    }
}