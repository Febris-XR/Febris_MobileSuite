// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.AdbLibrary.Interface
{
    public interface IAdbBase64
    {
		/**
	 * This function must encoded the specified data as a base 64 string, without
	 * appending any extra newlines or other characters.
	 * @param data Data to encode
	 * @return String containing base 64 encoded data
	 */
		string encodeToString(byte[] data);
	}
}
