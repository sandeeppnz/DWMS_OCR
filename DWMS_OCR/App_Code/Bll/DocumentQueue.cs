using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace DWMS_OCR.App_Code.Bll
{
    class DocumentQueue
    {
        private Queue _itemQueue = new Queue();

        public void EnqueueItem(object item)
        {
            lock (_itemQueue)
            {
                _itemQueue.Enqueue(item);
                Monitor.Pulse(_itemQueue);
            }
        }

        public object ConsumeItem()
        {
            object queueItem = null;
            lock (_itemQueue)
            {
                while (_itemQueue.Count == 0)
                {
                    Monitor.Wait(_itemQueue);
                    // Lock is released on _locker whilst waiting
                }

                // After Monitor is pulsed
                queueItem = _itemQueue.Dequeue();
                return queueItem;
            }

        }
    }
}
