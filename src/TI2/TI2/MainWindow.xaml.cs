using Microsoft.Win32;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace IT2
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

            if (showParts && bits.Length > 62)
            {
                // Показываем первые 62 бит
                sb.AppendLine("Первые 62 бит:");
                for (int i = 0; i < 62; i++)
                {
                    sb.Append(bits[i] ? '1' : '0');
                    if ((i + 1) % 8 == 0 && i < 61) sb.Append(' ');
                }

                sb.AppendLine("\n\nПоследние 62 бит:");
                // Показываем последние 62 бит
                for (int i = bits.Length - 62; i < bits.Length; i++)
                {
                    sb.Append(bits[i] ? '1' : '0');
                    if ((i - (bits.Length - 62) + 1) % 8 == 0 && i < bits.Length - 1) sb.Append(' ');
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

                        // Отображаем информацию о файле в GeneratedKeyBox
                        FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                        //GeneratedKeyBox.Text = $"Открыт файл: {fileInfo.Name}\n" +
                        //                     $"Размер: {fileInfo.Length} байт\n" +
                        //                     $"Время создания: {fileInfo.CreationTime}";
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
                MessageBox.Show("Нет данных для сохранения!", "Внимание");
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

                        MessageBox.Show($"Файл успешно сохранен: {saveFileDialog.FileName}\n", "Успешно");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении : {ex.Message}", "Ошибка");
                }
            }
        }
        #endregion

        #region Работа с главным меню
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "Приложение для потокового шифрования с использованием РСЛОС.\n\n" +
                              "Для корректной работы введите 31-битное начальное состояние регистра.";

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
            int registerLength = 31;
            if (initialState.Length != registerLength)
                throw new ArgumentException($"Длина начального состояния должна быть {registerLength} бит");

            // Преобразуем строку начального состояния в массив битов
            bool[] register = initialState.Select(c => c == '1').ToArray();

            // Биты обратной связи для полинома x^31 + x^3 + 1
            int[] feedbackBits = { 1, 29 }; // -2

            // Выходной массив битов
            BitArray keyStream = new BitArray(requiredBits);

            // Строки для отображения первых и последних 62 бит
            StringBuilder firstBits = new StringBuilder("Первые 62 вытолкнутых бит ключа:\n");
            StringBuilder lastBits = new StringBuilder("Последние 62 вытолкнутых бит ключа:\n");

            // Массив для хранения последних 62 бит
            bool[] lastKeyBits = new bool[62];
            int lastBitIndex = 0;

            for (int i = 0; i < requiredBits; i++)
            {
                // Сохраняем выходной бит (старший бит регистра)
                keyStream[i] = register[0];

                // Сохраняем первые 62 бит
                if (i < 62)
                {
                    firstBits.Append(register[0] ? '1' : '0');
                    // Добавляем пробел каждые 8 бит
                    if ((i + 1) % 8 == 0 && i < 61) firstBits.Append(' ');
                }

                // Сохраняем последние 62 бит в циклическом буфере
                lastKeyBits[lastBitIndex] = register[0];
                lastBitIndex = (lastBitIndex + 1) % 62;

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

            // Формируем строку с последними 62 битами
            for (int i = 0; i < 62; i++)
            {
                int idx = (lastBitIndex + i) % 31;
                lastBits.Append(lastKeyBits[idx] ? '1' : '0');
                // Добавляем пробел каждые 8 бит
                if ((i + 1) % 8 == 0 && i < 61) lastBits.Append(' ');
            }

            // Отображаем первые и последние 62 бит ключевого потока
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
        }

        private void Encrypt_Dec_Click(object sender, RoutedEventArgs e)
        {
            if (RegisterTextBox.Text.Length != 31)
            {
                MessageBox.Show("Длина вашего регистра должна равняться 31 состояниям!", "Внимание");
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

                // Отображаем первые и последние 62 бит в новом текстовом поле
                EncryptedTextBox.Text = BitArrayToStr(cipherBits, true);

                MessageBox.Show("Файл успешно зашифрован", "Успех");
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
            MessageBox.Show("Выполнено дешифрование с тем же ключом", "Дешифрование");
        }

        private void Clear_All_Click(object sender, RoutedEventArgs e)
        {
            RegisterTextBox.Text = string.Empty;
            SourceTextBox.Text = string.Empty;
            GeneratedKeyBox.Text = string.Empty;
            EncryptedTextBox.Text = string.Empty;

            // Очищаем также объекты для хранения данных
            streamCipher = new StreamCipher();
            SourceTextBox.Tag = null;
        }
        #endregion
    }
}