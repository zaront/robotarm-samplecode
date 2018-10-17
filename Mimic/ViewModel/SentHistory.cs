using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.ViewModel
{
    class SentHistory
    {
        List<string> _history = new List<string>();
        int _currentIndex;

        public void Add(string text)
        {
            //remove a re-issued command from history
            if (text == Current)
                _history.RemoveAt(_currentIndex);

            //add new entry
            _history.Add(text);

            //remove oldest command
            if (_history.Count > 100)
                _history.RemoveAt(0);

            //current index is 1 infront of the lattest command
            _currentIndex = _history.Count;
        }

        public string Current
        {
            get
            {
                //no history
                if (_history.Count == 0 || _currentIndex < 0)
                    return null;
                if (_currentIndex > _history.Count - 1)
                    return string.Empty;

                return _history[_currentIndex];

            }
        }

        public void MovePrev()
        {
            _currentIndex--;
            if (_currentIndex < 0)
                _currentIndex = 0;
        }

        public void MoveNext()
        {
            _currentIndex++;
            if (_currentIndex > _history.Count)
                _currentIndex = _history.Count;
        }

    }
}
