using System;
using System.Collections.Generic;
using System.Text;

namespace Node
{
    /// <summary>
    /// Events.
    /// </summary>
    public class SimpleUdpEvents
    {
        #region Public-Members

        /// <summary>
        /// The endpoint listener has started.
        /// </summary>
        public EventHandler Started;

        /// <summary>
        /// The endpoint listener has stopped.
        /// </summary>
        public EventHandler Stopped;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SimpleUdpEvents()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Internal-Methods

        internal void HandleStarted(object sender)
        {
            Started?.Invoke(sender, EventArgs.Empty);
        }

        internal void HandleStopped(object sender)
        {
            Stopped?.Invoke(sender, EventArgs.Empty);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
