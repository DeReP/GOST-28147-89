//Репнин Д. БАС11 2017

using System;
using System.IO;


namespace magma
{
    class Program
    {
        const int block_size = 4;
        
       // сложение 2 векторов длины block_size по модулю 2 
        static byte[] XOR_vect(byte[] a0, byte[] a1)   
        {
            byte[] res = new byte[block_size];
            int[] temp = new int[block_size];

            for (int i = 0; i < block_size; i++)
                res[i] = Convert.ToByte(a0[i] ^ a1[i]);
                
            return res;
        }


       // сложение 2 векторов длины block_size по модулю 32
        static byte[] Add_32(byte[] a0, byte[] a1) 
        {
            uint sum = 0;
            byte[] res = new byte[block_size];
            for(int i = block_size-1; i >= 0; i--)
            {
                sum =(uint)(a0[i] + a1[i] + (sum >> 8));  // сложение по модулю 32
                res[i] =(byte)(sum & 0xff);  // отбрасываем переполнение
            }

            return res;
        }


        //преобразование t (замена) 
        static byte[] Replace(byte[] data)
        {
            byte[,] table =
            {
                {1,7,14,13,0,5,8,3,4,15,10,6,9,12,11,2},
                {8,14,2,5,6,9,1,12,15,4,11,0,13,10,3,7},
                {5,13,15,6,9,2,12,10,11,7,8,1,4,3,14,0},
                {7,15,5,10,8,1,6,13,0,9,3,14,11,4,2,12},
                {12,8,2,1,13,4,15,6,7,0,10,5,3,14,9,11},
                {11,3,5,8,2,15,10,13,14,1,7,4,12,9,6,0},
                {6,8,2,3,9,10,5,12,1,14,4,7,11,13,0,15},
                {12,4,6,2,10,5,11,9,14,8,13,7,0,3,15,1}
            };

            byte a0, a1;
            byte[] res = new byte[block_size];
            for (int i = 0; i < block_size; i++)
            {
                a1 =(byte)((data[i] & 0xf0) >> 4); // берем первые 4 бита
                a0 = (byte)(data[i] & 0x0f);       //берем вторые 4 бита
                a1 = table[i * 2, a1];
                a0 = table[i * 2 + 1, a0];
                res[i] =(byte)((a1 << 4) | a0);   // соединение а1 и а0
            }

            return res;
        }


        // конвертация uint в массив 4 byte
        static byte[] Int_to_byte_arr(uint num)  
        {
            byte[] res = new byte[4];

            res[0] = Convert.ToByte(num >> 24);
            res[1] = Convert.ToByte((num & 0xff0000) >> 16);
            res[2] = Convert.ToByte((num & 0xff00) >> 8);
            res[3] = Convert.ToByte(num & 0xff);

            return res;
        }


        // разворачивание ключей как массив из 32 массивов по 4 byte 
        static byte[][] Deploy_keys(byte[] keys)  
        {
            byte[][] res = new byte[32][];
            for (int i = 0; i < 8; i += 1)
            {
                byte[] temp = new byte[4];
                //temp = Int_to_byte_arr(keys[i]);
                
                temp[0] = keys[i*4];
                temp[1] = keys[i*4 + 1];
                temp[2] = keys[i*4 + 2];
                temp[3] = keys[i*4 + 3];
                	

                res[i] = new byte[4];
                res[i] = temp;

                res[i + 8] = new byte[4];
                res[i + 8] = res[i];

                res[i + 16] = res[i];
                res[i + 16] = res[i];

                res[31 - i] = new byte[4];
                res[31 - i] = res[i];
                }

            return res;
        }

      
        //преобразование g
        static byte[] g_converting(byte[] a, byte[] k)
        {
            byte[] acc = new byte[4];
            uint temp;
            byte[] res = new byte[4];

            acc = Add_32(a, k); // складываем половину с итерационным ключом
            acc = Replace(acc); // замена
			//этот компилятор странно проиводит типы 
			temp =Convert.ToUInt32(((uint)acc[0] << 24) + (acc[1] << 16) + (acc[2] << 8) + acc[3]);  // создаем 1 четырехбайтное число из 4 однобайтных
            temp = (temp << 11) | (temp >> 21); // циклический сдвиг на 11 

            res = Int_to_byte_arr(temp);  // создаем массив из 4 однобайтных

            return res;
        }


        //преобразование G
        static byte[] G_converting(byte[] a, byte[] k, bool last_iter)
        {
            byte[] a0 = new byte[4];
            byte[] a1 = new byte[4];
            byte[] temp = new byte[4];
            byte[] res = new byte[8];

            //разбиваем на 2 половины
            for(int i = 0; i < 4; i++)
            {
                a1[i] = a[i];          
                a0[i] = a[i + 4];
            }

            //преобразование g
            temp = g_converting(a0, k);

              
            temp = XOR_vect(a1, temp);


            if (!last_iter) //если не последняя итерация делаем обмен между правой и левой частью
            {
                for (int i = 0; i < 4; i++)
                {
                    res[i] = a0[i];
                    res[i + 4] = temp[i];
                }
            }
            else //иначе не делаем обмен
            {
                for(int i = 0; i < 4; i++)
                {
                    res[i] = temp[i];
                    res[i + 4] = a0[i];
                }
            }
            return res;
        }


        //шифрование
        static byte[] Encrypt(byte[] data, byte[] primary_keys)
        {

            byte[] res = new byte[8];

            //развертывание ключа
            byte[][] keys = Deploy_keys(primary_keys);  
            

            res = G_converting(data, keys[0] , false);
            
            for(int i = 1; i < 31; i++)
               res = G_converting(res, keys[i], false);
            

            res = G_converting(res, keys[31], true);

            return res;
        }
        

        //расшифровывание
        static byte[] Decrypt(byte[] data, byte[] primary_keys)
        {
            byte[] res = new byte[8];
            byte[][] keys = Deploy_keys(primary_keys);

            res = G_converting(data, keys[31], false);

            for (int i = 30; i > 0; i--)
                res = G_converting(res, keys[i], false);

            res = G_converting(res, keys[0], true);

            return res;
        }
        
    }
}
