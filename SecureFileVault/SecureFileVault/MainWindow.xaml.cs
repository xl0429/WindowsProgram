using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SecureFileVault
{
    
    /*
     This application is a desktop app that allows users to encrypt and decrypt files with a password.
     Function: 
     -Drag and Drop of file is allow
     -User need to encrypt with a password in which there is an indicator with different color indicate its strength
     -AES256 with random salt is used for encrypted the file
     -The encrypted file will end with extension .enc, only .enc file is allowed to decrypted 
     */

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
                FilePathTextBox.Text = dialog.FileName;
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            string file = FilePathTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                byte[] salt = GenerateSalt();
                byte[] key = DeriveKey(password, salt);
                byte[] data = File.ReadAllBytes(file);
                byte[] encrypted = Encrypt(data, key, salt);

                File.WriteAllBytes(file + ".enc", encrypted);
                MessageBox.Show("Encryption successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error encrypting file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            string file = FilePathTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                byte[] data = File.ReadAllBytes(file);
                byte[] salt = new byte[32];
                Array.Copy(data, 0, salt, 0, 32);
                byte[] key = DeriveKey(password, salt);
                byte[] decrypted = Decrypt(data, key);

                string outPath = file.EndsWith(".enc") ? file.Replace(".enc", "") : file + "";
                File.WriteAllBytes(outPath, decrypted);
                MessageBox.Show("Decryption successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error decrypting file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FilePathTextBox.Text) || !File.Exists(FilePathTextBox.Text))
            {
                MessageBox.Show("Select a valid file.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Enter a password.");
                return false;
            }
            return true;
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var rfc = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            return rfc.GetBytes(32); // AES-256
        }

        private byte[] Encrypt(byte[] data, byte[] key, byte[] salt)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(salt, 0, salt.Length);     // 32 bytes
            ms.Write(aes.IV, 0, aes.IV.Length); // 16 bytes

            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }

        private byte[] Decrypt(byte[] data, byte[] key)
        {
            byte[] salt = new byte[32];
            byte[] iv = new byte[16];

            Array.Copy(data, 0, salt, 0, 32);
            Array.Copy(data, 32, iv, 0, 16);

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var msInput = new MemoryStream(data, 48, data.Length - 48);
            using var cs = new CryptoStream(msInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var msOutput = new MemoryStream();
            cs.CopyTo(msOutput);

            return msOutput.ToArray();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            int strength = CalculatePasswordStrength(PasswordBox.Password);
            PasswordStrengthBar.Value = strength;

            PasswordStrengthBar.Foreground = strength switch
            {
                < 30 => Brushes.Red,
                < 70 => Brushes.Orange,
                _ => Brushes.Green
            };
        }

        private int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return 0;

            int score = 0;

            // Length requirement (12+ is strong)
            if (password.Length >= 12)
                score += 30;
            else if (password.Length >= 8)
                score += 15;
            else
                return 10; // too short, weak

            // Character types
            if (Regex.IsMatch(password, @"[a-z]")) score += 15;          // lowercase
            if (Regex.IsMatch(password, @"[A-Z]")) score += 15;          // uppercase
            if (Regex.IsMatch(password, @"\d")) score += 15;             // digit
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>_\-+=]"))  // special chars
                score += 15;

            // Bonus for diverse character set
            if (Regex.IsMatch(password, @"[\s]")) score -= 10; // discourage whitespace

            return Math.Min(score, 100);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    FilePathTextBox.Text = files[0];
            }
        }
    }
}