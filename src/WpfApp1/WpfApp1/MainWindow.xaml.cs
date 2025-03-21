using Microsoft.Win32;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Класс для хранения битовых данных
        private class StreamCipher
        {
            public BitArray PlainText { get; set; }
            public BitArray CipherBit { get; set; }

            public StreamCipher()
            {
                PlainText = new BitArray(0);
                CipherBit = new BitArray(0);
            }
        }

        private StreamCipher streamCipher = new StreamCipher();

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Утилитарные методы

        // Метод для преобразования BitArray в строку для отображения
        private string BitArrayToStr(BitArray bits, bool showParts = true)
        {
            StringBuilder sb = new StringBuilder();

            if (showParts && bits.Length > 56)
            {
                // Показываем первые 56 бит
                sb.AppendLine("Первые 56 бит:");
                for (int i = 0; i < 56; i++)
                {
                    sb.Append(bits[i] ? '1' : '0');
                    if ((i + 1) % 8 == 0 && i < 55) sb.Append(' ');
                }

                sb.AppendLine("\n\nПоследние 56 бит:");
                // Показываем последние 56 бит
                for (int i = bits.Length - 56; i < bits.Length; i++)
                {
                    sb.Append(bits[i] ? '1' : '0');
                    if ((i - (bits.Length - 56) + 1) % 8 == 0 && i < bits.Length - 1) sb.Append(' ');
                }
            }
            else
            {
                // Если данных мало или showParts=false, показываем всё
                int count = 0;
                for (int i = 0; i < bits.Length; i++)
                {
                    sb.Append(bits[i] ? '1' : '0');
                    count++;

                    // Добавляем пробел каждые 8 бит для читаемости
                    if (count % 8 == 0) sb.Append(' ');

                    // Добавляем перевод строки каждые 64 бита для читаемости
                    if (count % 64 == 0) sb.Append('\n');
                }
            }

            return sb.ToString();
        }


        // Преобразование BitArray в массив байтов
        private byte[] BitArrayToByteArray(BitArray bits)
        {
            int byteCount = (bits.Length + 7) / 8; // Округляем вверх до целого байта
            byte[] bytes = new byte[byteCount];

            // Преобразуем биты в байты
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    bytes[i / 8] |= (byte)(1 << (7 - (i % 8))); // Старший бит идет первым
            }

            return bytes;
        }

        #endregion

        #region Работа с файлами
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Открыть файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fileStream = new FileStream(openFileDialog.FileName, FileMode.Open))
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        // Узнаем размер файла
                        long fileLength = fileStream.Length;
                        BitArray bitArray = new BitArray((int)(fileLength * 8));

                        // Буферизированное чтение
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        int bitIndex = 0;

                        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            for (int i = 0; i < bytesRead; i++)
                            {
                                // Для каждого байта сохраняем его биты по одному
                                for (int bit = 7; bit >= 0; bit--)  // с 7 до 0 для сохранения порядка - старший бит сначала
                                {
                                    bitArray[bitIndex++] = ((buffer[i] >> bit) & 1) == 1;
                                }
                            }
                        }

                        streamCipher.PlainText = bitArray;

                        // Отображаем двоичное представление файла
                        string binaryPreview = BitArrayToStr(bitArray);
                        SourceTextBox.Text = binaryPreview;
                        SourceTextBox.Tag = openFileDialog.FileName; // Сохраняем путь к файлу

                        // Отображаем информацию о файле
                        FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                        EncryptedTextLog.Items.Add($"Открыт файл: {fileInfo.Name}");
                        EncryptedTextLog.Items.Add($"Размер: {fileInfo.Length} байт");
                        EncryptedTextLog.Items.Add($"Время создания: {fileInfo.CreationTime}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка");
                }
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (streamCipher.CipherBit == null || streamCipher.CipherBit.Length == 0)
            {
                MessageBox.Show("Нет данных для сохранения. Сначала выполните шифрование/дешифрование.", "Внимание");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Сохранить файл"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        byte[] bytes = BitArrayToByteArray(streamCipher.CipherBit);
                        writer.Write(bytes);

                        EncryptedTextLog.Items.Add($"Файл сохранен: {saveFileDialog.FileName}");
                        EncryptedTextLog.Items.Add($"Размер: {bytes.Length} байт");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка");
                }
            }
        }
        #endregion

        #region Работа с главным меню
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "Приложение для потокового шифрования с использованием РСЛОС.\n\n" +
                              "Для корректной работы введите 28-битное начальное состояние регистра.";

            MessageBox.Show(aboutText, "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region РСЛОС и шифрование
        // Функция для генерации битов ключевого потока с помощью РСЛОС
        private BitArray GenerateKeyStream(string initialState, int requiredBits)
        {
            int registerLength = 28;
            if (initialState.Length != registerLength)
                throw new ArgumentException($"Длина начального состояния должна быть {registerLength} бит");

            // Преобразуем строку начального состояния в массив битов
            bool[] register = initialState.Select(c => c == '1').ToArray();

            // Биты обратной связи для полинома x^28 + x^3 + 1
            int[] feedbackBits = { 3, 28 };

            // Выходной массив битов
            BitArray keyStream = new BitArray(requiredBits);

            // Строки для отображения первых и последних 56 бит
            StringBuilder firstBits = new StringBuilder("Первые 56 вытолкнутых бит ключа:\n");
            StringBuilder lastBits = new StringBuilder("Последние 56 вытолкнутых бит ключа:\n");

            // Массив для хранения последних 56 бит
            bool[] lastKeyBits = new bool[56];
            int lastBitIndex = 0;

            for (int i = 0; i < requiredBits; i++)
            {
                // Сохраняем выходной бит (старший бит регистра)
                keyStream[i] = register[0];


                if (i < 56 || i >= requiredBits - 56)
                {
                    EncryptedTextLog.Items.Add($"Итерация {i + 1}: Вытолкнут бит {(register[0] ? '1' : '0')}");
                }
                else if (i == 56)
                {
                    EncryptedTextLog.Items.Add("...");
                }

                // Сохраняем первые 56 бит
                if (i < 56)
                {
                    firstBits.Append(register[0] ? '1' : '0');
                    // Добавляем пробел каждые 8 бит
                    if ((i + 1) % 8 == 0 && i < 55) firstBits.Append(' ');
                }

                // Сохраняем последние 56 бит в циклическом буфере
                lastKeyBits[lastBitIndex] = register[0];
                lastBitIndex = (lastBitIndex + 1) % 56;

                // Вычисляем новый бит обратной связи (XOR выбранных битов)
                bool newBit = false;
                foreach (int bitIndex in feedbackBits)
                {
                    if (bitIndex > 0 && bitIndex <= registerLength)
                        newBit ^= register[bitIndex - 1];
                }

                // Сдвигаем регистр влево
                for (int j = 0; j < registerLength - 1; j++)
                {
                    register[j] = register[j + 1];
                }

                // Вставляем новый бит справа
                register[registerLength - 1] = newBit;
            }

            // Формируем строку с последними 56 битами
            for (int i = 0; i < 56; i++)
            {
                int idx = (lastBitIndex + i) % 28;
                lastBits.Append(lastKeyBits[idx] ? '1' : '0');
                // Добавляем пробел каждые 8 бит
                if ((i + 1) % 8 == 0 && i < 55) lastBits.Append(' ');
            }

            // Отображаем первые и последние 56 бит ключевого потока
            GeneratedKeyBox.Text = firstBits.ToString() + "\n\n" + lastBits.ToString();

            return keyStream;
        }


        private void RegisterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string filteredText = string.Join("", RegisterTextBox.Text.Where(x => x == '0' || x == '1'));

            // Если текст изменился после фильтрации, обновляем его без вызова события
            if (RegisterTextBox.Text != filteredText)
            {
                // Запоминаем позицию курсора
                int caretPosition = RegisterTextBox.CaretIndex;
                RegisterTextBox.Text = filteredText;
                // Пытаемся восстановить позицию курсора
                if (caretPosition <= filteredText.Length)
                    RegisterTextBox.CaretIndex = caretPosition;
                else
                    RegisterTextBox.CaretIndex = filteredText.Length;
            }

            // Обновляем метку с текущим количеством состояний
            int currentLength = RegisterTextBox.Text.Length;
            RegisterStateLabel.Content = $"Состояние регистра. Необходимо 28 состояний. Сейчас - {currentLength}.";
        }

        private void Encrypt_Dec_Click(object sender, RoutedEventArgs e)
        {
            if (RegisterTextBox.Text.Length != 28)
            {
                MessageBox.Show("Длина вашего регистра должна равняться 28 состояниям!", "Внимание");
                return;
            }

            if (streamCipher.PlainText == null || streamCipher.PlainText.Length == 0)
            {
                MessageBox.Show("Выберите файл для шифрования!", "Внимание");
                return;
            }

            try
            {
                // Генерация ключевого потока
                BitArray keyStream = GenerateKeyStream(RegisterTextBox.Text, streamCipher.PlainText.Length);

                // Выполняем XOR между исходными данными и ключевым потоком
                BitArray cipherBits = new BitArray(streamCipher.PlainText);
                cipherBits.Xor(keyStream);

                // Сохраняем результат
                streamCipher.CipherBit = cipherBits;

                // Отображаем первые и последние 56 бит в новом текстовом поле
                EncryptedTextBox.Text = BitArrayToStr(cipherBits, true);

                // Выводим информацию в лог
                EncryptedTextLog.Items.Add("Файл успешно зашифрован");
                EncryptedTextLog.Items.Add($"Использован ключ: {RegisterTextBox.Text}");
                // Убираем дублирование в логе, так как теперь биты выводятся в отдельном поле
                EncryptedTextLog.Items.Add("Зашифрованные данные отображены в основном окне");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при шифровании: {ex.Message}", "Ошибка");
            }
        }


        private void Decrypt_Dec_Click(object sender, RoutedEventArgs e)
        {
            // Для потокового шифра операция дешифрования идентична шифрованию
            // т.к. XOR выполняется повторно с тем же ключом
            Encrypt_Dec_Click(sender, e);
            EncryptedTextLog.Items.Add("Выполнено дешифрование с тем же ключом");
        }

        private void Clear_All_Click(object sender, RoutedEventArgs e)
        {
            RegisterTextBox.Text = string.Empty;
            SourceTextBox.Text = string.Empty;
            GeneratedKeyBox.Text = string.Empty;
            EncryptedTextBox.Text = string.Empty; // Добавляем очистку нового поля
            EncryptedTextLog.Items.Clear();

            // Очищаем также объекты для хранения данных
            streamCipher = new StreamCipher();
            SourceTextBox.Tag = null;
        }

        #endregion
    }
}
