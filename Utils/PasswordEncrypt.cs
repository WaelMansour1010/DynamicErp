using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MyERP
{
    public class PasswordEncrypt
    {
        Random rGen;

        //---------------------------------------------------------------------------------------

        public PasswordEncrypt(): this((int)DateTime.Now.Ticks)
        {
        }

        //---------------------------------------------------------------------------------------

        public PasswordEncrypt(int seed)
        {
            rGen = new Random(seed);
        }

        //---------------------------------------------------------------------------------------

        public string GenerateRandomID(int pwdLength, string prefix)
        {
            // TODO: At some later date it would be nice to have a field in the RC's
            //		 preferences that is a bit mask of ID options. These might include
            //			*	Make UserID / password the same (not handled here, though).
            //				*	Hmmm, maybe we have some overloads on this routine to return
            //					both a UserID and a password, so we could handle it here)
            //			*	Upper case only
            //			*	Lower case only (the current default)
            //			*	Mixed upper and lower case (e.g. pw=abcDEFgh)
            //			*	Allow ambiguous strings (i.e. with 0/o/O, 1/i/I/l/L)
            //		 Etc
            int p = 0;
            string str = "";
            // As per CDS's request, we've removed ambiguous characters. For example, there's
            // no "0", "o", "O". Or "1", "i", "I", "l" or "L".
            string IDChars = "abcdefghjkmnpqrstuvwxyz23456789";
            int IDCharsCount = IDChars.Length;
            for (int i = 0; i < pwdLength; i++)
            {
                p = rGen.Next(0, IDCharsCount - 1);
                str += IDChars[p];
            }
            return prefix + str;
        }

        //---------------------------------------------------------------------------------------

        public string GenerateRandomID(int pwdLength)
        {
            return GenerateRandomID(pwdLength, "");
        }

        //---------------------------------------------------------------------------------------

        public static string ComputeHashPwd(string strPwd)
        {
            return ComputeHashPwd(strPwd, null);
        }

        //---------------------------------------------------------------------------------------

        public static string ComputeHashPwd(string strPwd, byte[] saltBytes)
        {
            // If salt is not specified, we will generate one
            if (saltBytes == null)
            {
                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(4, 8);
                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];
                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            // Convert plain text into a byte array.
            byte[] strPwdBytes = Encoding.UTF8.GetBytes(strPwd);

            // Allocate array, which will hold plain text and salt.
            byte[] strPwdWithSaltBytes = new byte[strPwdBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < strPwdBytes.Length; i++)
                strPwdWithSaltBytes[i] = strPwdBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                strPwdWithSaltBytes[strPwdBytes.Length + i] = saltBytes[i];

            // We will use the md5 hash algorithm
            HashAlgorithm hash = new MD5CryptoServiceProvider();

            // Compute hash value of our plain text with appended salt.
            byte[] hashBytes = hash.ComputeHash(strPwdWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            // Append salt bytes to the result.
            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

        //---------------------------------------------------------------------------------------

        public static bool VerifyHashPwd(string inputPwd, string storedHashValue)
        {

            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Convert.FromBase64String(storedHashValue);

            // We must know size of hash (without salt).
            int hashSizeInBytes = 16; // We are using md5 encryption 16 = 128/8

            // Make sure that the specified hash value is bigger or equal hashSizeInBytes.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            byte[] saltBytes = new byte[hashWithSaltBytes.Length -
                                        hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

            // Compute a new hash string.
            string expectedHashString = ComputeHashPwd(inputPwd, saltBytes);

            return storedHashValue == expectedHashString;
        }

        public static string DecHashPwd(string inputPwd, string storedHashValue)
        {

            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Convert.FromBase64String(storedHashValue);

            // We must know size of hash (without salt).
            int hashSizeInBytes = 16; // We are using md5 encryption 16 = 128/8

            // Make sure that the specified hash value is bigger or equal hashSizeInBytes.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return "";

            // Allocate array to hold original salt bytes retrieved from hash.
            byte[] saltBytes = new byte[hashWithSaltBytes.Length -
                                        hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

            // Compute a new hash string.
            string expectedHashString = ComputeHashPwd(inputPwd, saltBytes);

            return expectedHashString;
        }
    }
}