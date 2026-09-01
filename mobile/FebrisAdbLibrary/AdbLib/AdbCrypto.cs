// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Febris.AdbLibrary.Interface;
using Java.Interop;
using Java.IO;
using Java.Math;
using Java.Nio;
using Java.Security;
using Java.Security.Interfaces;
using Java.Security.Spec;
using Javax.Crypto;
using System;
using System.Text;

namespace Febris.AdbLibrary.AdbLib
{
    /**
 * This class encapsulates the ADB cryptography functions and provides
 * an interface for the storage and retrieval of keys.
 * @author Cameron Gutman
 */
    public class AdbCrypto//:Java.Lang.Object
    {
        #region constructor and variables
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbCrypto");

        /** An RSA keypair encapsulated by the AdbCrypto object */
        //private KeyPair keyPair;
        private KeyPair _keyPair { get; set; }
        
        //private IRSAPublicKey PublicKey;
        //private IRSAPrivateKey PrivateKey;


        /** The base 64 conversion interface to use */
        private IAdbBase64 _base64 { get; set; }

        /** The ADB RSA key length in bits */
        public const int KEY_LENGTH_BITS = 2048;

        /** The ADB RSA key length in bytes */
        public static int KEY_LENGTH_BYTES = KEY_LENGTH_BITS / 8;

        /** The ADB RSA key length in words */
        public static int KEY_LENGTH_WORDS = KEY_LENGTH_BYTES / 4;

        /** The RSA signature padding as an int array */
        public static int[] SIGNATURE_PADDING_AS_INT = new int[]
                {
            0x00,0x01,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0x00,
            0x30,0x21,0x30,0x09,0x06,0x05,0x2b,0x0e,0x03,0x02,0x1a,0x05,0x00,
            0x04,0x14
                };

        /** The RSA signature padding as a byte array */
        public static byte[] SIGNATURE_PADDING;

        public AdbCrypto(IAdbBase64 base64)
        {
            _base64 = base64;
            CreateSignaturePadding();
        }
        #endregion

        #region Key Cypher
        /**
         * Converts a standard RSAPublicKey object to the special ADB format
         * @param pubkey RSAPublicKey object to convert
         * @return Byte array containing the converted RSAPublicKey object
         */
        #region Current
        private static byte[] ConvertRsaPublicKeyToAdbFormat(IPublicKey publicKey)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.ConvertRsaPublicKeyToAdbFormat");
            IRSAPublicKey pubkey = publicKey.JavaCast<IRSAPublicKey>();
            var e = pubkey.PublicExponent;
            var m = pubkey.Modulus;

            /*
             * ADB literally just saves the RSAPublicKey struct to a file.
             * 
             * typedef struct RSAPublicKey {
             * int len; // Length of n[] in number of uint32_t
             * uint32_t n0inv;  // -1 / n[0] mod 2^32
             * uint32_t n[RSANUMWORDS]; // modulus as little endian array
             * uint32_t rr[RSANUMWORDS]; // R^2 as little endian array
             * int exponent; // 3 or 65537
             * } RSAPublicKey;
             */

            /* ------ This part is a Java-ified version of RSA_to_RSAPublicKey from adb_host_auth.c ------ */
            
            try
            {
                BigInteger r32;
                BigInteger r;
                BigInteger rr;
                BigInteger rem;
                BigInteger n;
                BigInteger n0inv;

                r32 = BigInteger.Zero.SetBit(32);
                n = pubkey.Modulus;
                r = BigInteger.Zero.SetBit(KEY_LENGTH_WORDS * 32);
                rr = r.ModPow(BigInteger.ValueOf(2), n);
                rem = n.Remainder(r32);
                n0inv = rem.ModInverse(r32);

                int[] myN = new int[KEY_LENGTH_WORDS];
                int[] myRr = new int[KEY_LENGTH_WORDS];
                BigInteger[] res = default;
                //[];
                for (int i = 0; i < KEY_LENGTH_WORDS; i++)
                {
                    res = rr.DivideAndRemainder(r32);
                    rr = res[0];
                    rem = res[1];
                    myRr[i] = rem.IntValue();

                    res = n.DivideAndRemainder(r32);
                    n = res[0];
                    rem = res[1];
                    myN[i] = rem.IntValue();
                }

                /* ------------------------------------------------------------------------------------------- */

                ByteBuffer bbuf = ByteBuffer.Allocate(524).Order(ByteOrder.LittleEndian);


                bbuf.PutInt(KEY_LENGTH_WORDS);
                bbuf.PutInt(n0inv.Negate().IntValue());
                foreach (int i in myN)
                    bbuf.PutInt(i);
                foreach (int i in myRr)
                    bbuf.PutInt(i);

                bbuf.PutInt(pubkey.PublicExponent.IntValue());
                byte[] output = Helpers.ConvertByteBufferToArray(bbuf);
                return output;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto ConvertRsaPublicKeyToAdbFormat: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto ConvertRsaPublicKeyToAdbFormat: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto ConvertRsaPublicKeyToAdbFormat: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto ConvertRsaPublicKeyToAdbFormat: " + ex.StackTrace);
                throw;
            }
            return default;
        }
        #endregion
        
        #endregion

        #region Key Creation

        /**
         * Creates a new AdbCrypto object by generating a new key pair.
         * @param base64 Implementation of base 64 conversion interface required by ADB
         * @return A new AdbCrypto object
         * @throws java.security.NoSuchAlgorithmException If an RSA key factory cannot be found
         */        
        public void GenerateAdbKeyPair()
        {
            //LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");
            try
            {
                KeyPairGenerator rsaKeyPg = KeyPairGenerator.GetInstance("RSA");
                rsaKeyPg.Initialize(KEY_LENGTH_BITS);

                _keyPair = rsaKeyPg.GenKeyPair();
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto GenerateAdbKeyPair: " + ex.StackTrace);
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto GenerateAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto GenerateAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto GenerateAdbKeyPair: " + ex.StackTrace);
                throw;
            }            
        }

        /// <summary>
        /// Creates the expected padding for the keys
        /// </summary>
        public static void CreateSignaturePadding()
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");
            try
            {
                SIGNATURE_PADDING = new byte[SIGNATURE_PADDING_AS_INT.Length];
                for (int i = 0; i < SIGNATURE_PADDING.Length; i++)
                {
                    SIGNATURE_PADDING[i] = (byte)SIGNATURE_PADDING_AS_INT[i];
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto CreateSignaturePadding: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto CreateSignaturePadding: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto CreateSignaturePadding: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto CreateSignaturePadding: " + ex.StackTrace);
                throw;
            }
        }

        #endregion

        #region Key manipulation

        /**
        * Signs the ADB SHA1 payload with the private key of this object.
        * @param payload SHA1 payload to sign
        * @return Signed SHA1 payload
        * @throws java.security.GeneralSecurityException If signing fails
        */
        public byte[] SignAdbTokenPayload(byte[] payload)
        {
            try
            {
                Cipher c = Cipher.GetInstance("RSA/ECB/NoPadding");

                c.Init(CipherMode.EncryptMode, _keyPair.Private);

                c.Update(SIGNATURE_PADDING);

                return c.DoFinal(payload);
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto SignAdbTokenPayload: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto SignAdbTokenPayload: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto SignAdbTokenPayload: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto SignAdbTokenPayload: " + ex.StackTrace);
                throw;
            }
            return default;
        }


        #endregion

        #region Key Storage
        /// <summary>
        /// Simple test to check the existance in file system
        /// </summary>
        /// <returns></returns>
        public bool KeyPairExists()
        {
            try
            {
                bool output = false;
                if (_keyPair!=default && _keyPair.Private != default && _keyPair.Public != default)
                {
                    output = true;
                }
                return output;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto KeyPairExists: " + ex.StackTrace);
                //throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto KeyPairExists: " + ex.StackTrace);
                //throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto KeyPairExists: " + ex.StackTrace);
                //throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto KeyPairExists: " + ex.StackTrace);
                //throw;
            }

            return default;
        }

        /**
         * Creates a new AdbCrypto object from a key pair loaded from files.
         * @param base64 Implementation of base 64 conversion interface required by ADB 
         * @param privateKey File containing the RSA private key
         * @param publicKey File containing the RSA public key
         * @return New AdbCrypto object
         * @throws java.io.IOException If the files cannot be read
         * @throws java.security.NoSuchAlgorithmException If an RSA key factory cannot be found
         * @throws java.security.spec.InvalidKeySpecException If a PKCS8 or X509 key spec cannot be found
         */
        public void LoadAdbKeyPair(File privateKey, File publicKey)
        {
            //LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");
            try
            {                
                int privKeyLength = (int)privateKey.Length();
                int pubKeyLength = (int)publicKey.Length();
                byte[] privKeyBytes = new byte[privKeyLength];
                byte[] pubKeyBytes = new byte[pubKeyLength];

                FileInputStream privIn = new FileInputStream(privateKey);
                FileInputStream pubIn = new FileInputStream(publicKey);

                // FIX (MDM-B10): Read full ADB key file. FileInputStream.Read may return fewer bytes than requested, so loop until the buffer is filled. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                //privIn.Read(privKeyBytes);
                //pubIn.Read(pubKeyBytes);
                int privTotalRead = 0;
                while (privTotalRead < privKeyLength)
                {
                    int n = privIn.Read(privKeyBytes, privTotalRead, privKeyLength - privTotalRead);
                    if (n <= 0) throw new IOException("Short read");
                    privTotalRead += n;
                }
                int pubTotalRead = 0;
                while (pubTotalRead < pubKeyLength)
                {
                    int n = pubIn.Read(pubKeyBytes, pubTotalRead, pubKeyLength - pubTotalRead);
                    if (n <= 0) throw new IOException("Short read");
                    pubTotalRead += n;
                }

                privIn.Close();
                pubIn.Close();

                KeyFactory keyFactory = KeyFactory.GetInstance("RSA");
                EncodedKeySpec privateKeySpec = new PKCS8EncodedKeySpec(privKeyBytes);
                EncodedKeySpec publicKeySpec = new X509EncodedKeySpec(pubKeyBytes);

                _keyPair = new KeyPair(keyFactory.GeneratePublic(publicKeySpec),
                        keyFactory.GeneratePrivate(privateKeySpec));                
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto LoadAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto LoadAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto LoadAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto LoadAdbKeyPair: " + ex.StackTrace);
                throw;
            }            
        }

       
        /**
         * Gets the RSA public key in ADB format.
         * @return Byte array containing the RSA public key in ADB format.
         * @throws java.io.IOException If the key cannot be retrived
         */
        public byte[] GetAdbPublicKeyPayload() ////throws IOException
        {
            try
            {
                ///Get Public key into a usable format
                IPublicKey rsaPubKey = _keyPair.Public;
                byte[] convertedKey = ConvertRsaPublicKeyToAdbFormat(rsaPubKey);

                ///Use the byte array to build out ADB usable array
                StringBuilder keyString = new StringBuilder(720);
                /* The key is base64 encoded with a user@host suffix and terminated with a NUL */
                keyString.Append(_base64.encodeToString(convertedKey));
                keyString.Append(" unknown@unknown");
                keyString.Append(char.MinValue);

                byte[] output = Helpers.StringToBytes(keyString.ToString());
                return output;
            }            
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto GetAdbPublicKeyPayload: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto GetAdbPublicKeyPayload: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto GetAdbPublicKeyPayload: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto GetAdbPublicKeyPayload: " + ex.StackTrace);
                throw;
            }
            return default;
        }

        /**
         * Saves the AdbCrypto's key pair to the specified files.
         * @param privateKey The file to store the encoded private key
         * @param publicKey The file to store the encoded public key
         * @throws java.io.IOException If the files cannot be written
         */
        public void SaveAdbKeyPair(File privateKey, File publicKey)
        {
            try
            {
                FileOutputStream privOut = new FileOutputStream(privateKey);
                FileOutputStream pubOut = new FileOutputStream(publicKey);

                privOut.Write(_keyPair.Private.GetEncoded());
                pubOut.Write(_keyPair.Public.GetEncoded());

                privOut.Close();
                pubOut.Close();
            }            
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbCrypto SaveAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in  AdbCrypto SaveAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbCrypto SaveAdbKeyPair: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbCrypto SaveAdbKeyPair: " + ex.StackTrace);
                throw;
            }
        }
        #endregion
    }
}
