﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toxy.Common
{
    /// <summary>
    /// The personal folders existing since Windows Vista.
    /// </summary>
    public enum UserFolder
    {
        /// <summary>
        /// The Contacts folder.
        /// </summary>
        Contacts,

        /// <summary>
        /// The Desktop folder.
        /// </summary>
        Desktop,

        /// <summary>
        /// The Documents folder.
        /// </summary>
        Documents,

        /// <summary>
        /// The Downloads folder.
        /// </summary>
        Downloads,

        /// <summary>
        /// The Favorites folder.
        /// </summary>
        Favorites,

        /// <summary>
        /// The Links folder.
        /// </summary>
        Links,

        /// <summary>
        /// The music folder.
        /// </summary>
        Music,

        /// <summary>
        /// The Pictures folder.
        /// </summary>
        Pictures,

        /// <summary>
        /// The Saved Games folder.
        /// </summary>
        SavedGames,

        /// <summary>
        /// The Searches folder.
        /// </summary>
        Searches,

        /// <summary>
        /// The Videos folder.
        /// </summary>
        Videos
    }

    /// <summary>
    /// Class containing methods to retrieve specific file system paths since Windows Vista.
    /// </summary>
    public static class VistaPaths
    {
        private const string _userFolderKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";

        private static string[] _userFolderKeyNames = new string[]
        {
            "{56784854-C6CB-462B-8169-88E350ACB882}", // Contacts
            "Desktop",                                // Desktop
            "Personal",                               // Documents
            "{374DE290-123F-4565-9164-39C4925E467B}", // Downloads
            "Favorites",                              // Favorites
            "{BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968}", // Links
            "My Music",                               // Music
            "My Pictures",                            // Pictures
            "{4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4}", // Saved Games
            "{7D1D3A04-DEBB-4115-95CF-2F29DA2920DA}", // Searches
            "My Video"                                // Videos
        };

        /// <summary>
        /// Gets the user defined path of a personal folder in Windows Vista and later operating systems.
        /// </summary>
        /// <param name="userFolder">The personal folder to retrieve.</param>
        /// <returns>The path of the personal folder or an empty string if its path was not specified.</returns>
        public static string GetUserFolderPath(UserFolder userFolder)
        {
            return Registry.GetValue(_userFolderKey, _userFolderKeyNames[(int)userFolder], String.Empty).ToString();
        }
    }
}
