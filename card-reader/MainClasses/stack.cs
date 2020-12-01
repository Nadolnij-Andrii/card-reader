using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class stack
    {
        private byte[] arr;
        private int tos = 0;
        public stack(int size)
        {
            this.arr = new byte[size];
        }
        public byte[] get()
        {
            return this.arr;
        }
        public void clear()
        {
            this.arr = new byte[this.arr.Length];
            this.tos = 0;
        }
        public void push(byte item)
        {
            if ((this.tos + 1) >= this.arr.Length)
            {
                var len = this.arr.Length;
                byte[] tmp = new byte[len];
                Array.Copy(this.arr, tmp, len);
                this.arr = new byte[len + 30];
                Array.Copy(tmp, this.arr, len);
            }
            this.arr[this.tos++] = item;
        }

        public byte pop()
        {
            return this.arr[this.tos--];
        }
    }
}
