﻿using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using Microsoft.Win32;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetItemPropertyCommand : PSCommand
    {
        public GetItemPropertyCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            bool includeHidden = _arguments.Get<BoolArgument>("Force").Value;
            string path = _arguments.Get<StringArgument>("Path").Value;
            string searchPattern = _arguments.Get<StringArgument>("Include").Value;
            string checkPath = path.ToUpperInvariant();

            // Registry:
            //     HKLM:\
            //     HKCU:\
            //     HKCR:\
            //     HKU:\
            if (checkPath.StartsWith("HKLM:") || checkPath.StartsWith("HKCU:") || checkPath.StartsWith("HKCR:") || checkPath.StartsWith("HKU:"))
                _results = BrowseRegistry(path, includeHidden);

            // Filesystem:
            //     \
            //     ..\
            //     D:\
            else
                _results = GetChildItemCommand.BrowseFilesystem(path, false, includeHidden, searchPattern);

            return _results;
        }

        private CommandResult BrowseRegistry(string path, bool includeHidden)
        {
            RegistryKey root = null;
            string newPath = string.Empty;
            path = path.ToUpperInvariant();
            if (path.StartsWith("HKLM:"))
            {
                root = Registry.LocalMachine;
                newPath = path.Replace("HKLM:", string.Empty);
            }
            else if (path.StartsWith("HKCU:"))
            {
                root = Registry.CurrentUser;
                newPath = path.Replace("HKCU:", string.Empty);
            }
            else if (path.StartsWith("HKCR:"))
            {
                root = Registry.ClassesRoot;
                newPath = path.Replace("HKCR:", string.Empty);
            }
            else if (path.StartsWith("HKU:"))
            {
                root = Registry.Users;
                newPath = path.Replace("HKU:", string.Empty);
            }
            else
                throw new InvalidOperationException("Unknown registry path.");

            if (newPath.StartsWith(@"\"))
                newPath = newPath.Substring(1);

            RegistryKey key = root.OpenSubKey(newPath);
            foreach (string valueName in key.GetValueNames())
            {
                string valueKind = key.GetValueKind(valueName).ToString();
                string value = Convert.ToString(key.GetValue(valueName));

                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", valueName },
                        { "Kind", valueKind },
                        { "Value", value }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ItemProperty", "gp" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path", "."),
                    new BoolArgument("Force") ,
                    new StringArgument("Include", "*", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the properties of a specified item."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List autoruns in the registry", "Get-ItemProperty HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run | ft")
                };
            }
        }
    }
}
