﻿// License: Apache-2.0 License
// https://github.com/melenaos/FileSystemSafeWatcher

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using Timer = System.Timers.Timer;

namespace VModer.Core.Infrastructure;

// DISCLAIMER: Use this code at your own risk.
// No support is provided and this code has NOT been tested.

/// <summary>
/// This class wraps a FileSystemWatcher object. The class is not derived
/// from FileSystemWatcher because most of the FileSystemWatcher methods
/// are not virtual. The class was designed to resemble FileSystemWatcher class
/// as much as possible so that you can use FileSystemSafeWatcher instead
/// of FileSystemWatcher objects.
/// FileSystemSafeWatcher will capture all events from the FileSystemWatcher object.
/// The captured events will be delayed by at least ConsolidationInterval milliseconds in order
/// to be able to eliminate duplicate events. When duplicate events are found, the last event
/// is droped and the first event is fired (the reverse is not recomended because it could
/// cause some events not be fired at all since the last event will become the first event and
/// it won't fire a if a new similar event arrives imediately afterwards).
/// </summary>
/// <remarks>
/// 如果使用 <see cref="FileSystemWatcher"/>, 会多次触发 Changed 事件, 在拷贝文件时, 也会多次触发 Changed 事件
/// </remarks>
public sealed class FileSystemSafeWatcher : IDisposable
{
    private readonly FileSystemWatcher _fileSystemWatcher;

    /// <summary>
    /// Lock order is _lock, _events.SyncRoot
    /// </summary>
    private readonly Lock _lock = new(); // Only one timer event is processed at any given moment
    private ArrayList _events;

    private Timer _serverTimer;
    private int _consolidationInterval = 700; // milliseconds
    #region Delegate to FileSystemWatcher

    public FileSystemSafeWatcher()
    {
        _fileSystemWatcher = new FileSystemWatcher();
        Initialize();
    }

    public FileSystemSafeWatcher(string path)
    {
        _fileSystemWatcher = new FileSystemWatcher(path);
        Initialize();
    }

    public FileSystemSafeWatcher(string path, string filter)
    {
        _fileSystemWatcher = new FileSystemWatcher(path, filter);
        Initialize();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the component is enabled.
    /// </summary>
    /// <value>true if the component is enabled; otherwise, false. The default is false. If you are using the component on a designer in Visual Studio 2005, the default is true.</value>
    public bool EnableRaisingEvents
    {
        get => _fileSystemWatcher.EnableRaisingEvents;
        set
        {
            _fileSystemWatcher.EnableRaisingEvents = value;
            if (value)
            {
                _serverTimer.Start();
            }
            else
            {
                _serverTimer.Stop();
                _events.Clear();
            }
        }
    }

    /// <summary>
    /// Gets or sets the filter string, used to determine what files are monitored in a directory.
    /// </summary>
    /// <value>The filter string. The default is "*.*" (Watches all files.)</value>
    public string Filter
    {
        get => _fileSystemWatcher.Filter;
        set => _fileSystemWatcher.Filter = value;
    }

    public Collection<string> Filters => _fileSystemWatcher.Filters;

    /// <summary>
    /// Gets or sets a value indicating whether subdirectories within the specified path should be monitored.
    /// </summary>
    /// <value>true if you want to monitor subdirectories; otherwise, false. The default is false.</value>
    public bool IncludeSubdirectories
    {
        get => _fileSystemWatcher.IncludeSubdirectories;
        set => _fileSystemWatcher.IncludeSubdirectories = value;
    }

    /// <summary>
    /// Gets or sets the size of the internal buffer.
    /// </summary>
    /// <value>The internal buffer size. The default is 8192 (8K).</value>
    public int InternalBufferSize
    {
        get => _fileSystemWatcher.InternalBufferSize;
        set => _fileSystemWatcher.InternalBufferSize = value;
    }

    /// <summary>
    /// Gets or sets the type of changes to watch for.
    /// </summary>
    /// <value>One of the System.IO.NotifyFilters values. The default is the bitwise OR combination of LastWrite, FileName, and DirectoryName.</value>
    /// <exception cref="System.ArgumentException">The value is not a valid bitwise OR combination of the System.IO.NotifyFilters values.</exception>
    public NotifyFilters NotifyFilter
    {
        get => _fileSystemWatcher.NotifyFilter;
        set => _fileSystemWatcher.NotifyFilter = value;
    }

    /// <summary>
    /// Gets or sets the path of the directory to watch.
    /// </summary>
    /// <value>The path to monitor. The default is an empty string ("").</value>
    /// <exception cref="System.ArgumentException">The specified path contains wildcard characters.-or- The specified path contains invalid path characters.</exception>
    public string Path
    {
        get => _fileSystemWatcher.Path;
        set => _fileSystemWatcher.Path = value;
    }

    /// <summary>
    /// Gets or sets the object used to marshal the event handler calls issued as a result of a directory change.
    /// </summary>
    /// <value>The System.ComponentModel.ISynchronizeInvoke that represents the object used to marshal the event handler calls issued as a result of a directory change. The default is null.</value>
    public ISynchronizeInvoke? SynchronizingObject
    {
        get => _fileSystemWatcher.SynchronizingObject;
        set => _fileSystemWatcher.SynchronizingObject = value;
    }

    /// <summary>
    /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is changed.
    /// </summary>
    public event FileSystemEventHandler? Changed;

    /// <summary>
    /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is created.
    /// </summary>
    public event FileSystemEventHandler? Created;

    /// <summary>
    /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is deleted.
    /// </summary>
    public event FileSystemEventHandler? Deleted;

    /// <summary>
    /// Occurs when the internal buffer overflows.
    /// </summary>
    public event ErrorEventHandler? Error;

    /// <summary>
    /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is renamed.
    /// </summary>
    public event RenamedEventHandler? Renamed;

    /// <summary>
    /// Begins the initialization of a System.IO.FileSystemWatcher used on a form or used by another component. The initialization occurs at run time.
    /// </summary>
    public void BeginInit()
    {
        _fileSystemWatcher.BeginInit();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the System.IO.FileSystemWatcher and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        Uninitialize();
    }

    /// <summary>
    /// Ends the initialization of a System.IO.FileSystemWatcher used on a form or used by another component. The initialization occurs at run time.
    /// </summary>
    public void EndInit()
    {
        _fileSystemWatcher.EndInit();
    }

    /// <summary>
    /// Raises the System.IO.FileSystemWatcher.Changed event.
    /// </summary>
    /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
    private void OnChanged(FileSystemEventArgs e)
    {
        Changed?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the System.IO.FileSystemWatcher.Created event.
    /// </summary>
    /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
    private void OnCreated(FileSystemEventArgs e)
    {
        Created?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the System.IO.FileSystemWatcher.Deleted event.
    /// </summary>
    /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
    private void OnDeleted(FileSystemEventArgs e)
    {
        Deleted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the System.IO.FileSystemWatcher.Error event.
    /// </summary>
    /// <param name="e">An System.IO.ErrorEventArgs that contains the event data.</param>
    private void OnError(ErrorEventArgs e)
    {
        Error?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the System.IO.FileSystemWatcher.Renamed event.
    /// </summary>
    /// <param name="e">A System.IO.RenamedEventArgs that contains the event data.</param>
    private void OnRenamed(RenamedEventArgs e)
    {
        Renamed?.Invoke(this, e);
    }

    /// <summary>
    /// A synchronous method that returns a structure that contains specific information on the change that occurred, given the type of change you want to monitor.
    /// </summary>
    /// <param name="changeType">The System.IO.WatcherChangeTypes to watch for.</param>
    /// <returns>A System.IO.WaitForChangedResult that contains specific information on the change that occurred.</returns>
    public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// A synchronous method that returns a structure that contains specific information on the change that occurred, given the type of change you want to monitor
    /// and the time (in milliseconds) to wait before timing out.
    /// </summary>
    /// <param name="changeType">The System.IO.WatcherChangeTypes to watch for.</param>
    /// <param name="timeout">The time (in milliseconds) to wait before timing out.</param>
    /// <returns>A System.IO.WaitForChangedResult that contains specific information on the change that occurred.</returns>
    public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
    {
        throw new NotSupportedException();
    }

    #endregion

    #region Implementation

    [MemberNotNull(nameof(_events), nameof(_serverTimer))]
    private void Initialize()
    {
        _events = ArrayList.Synchronized(new ArrayList(32));
        _fileSystemWatcher.Changed += FileSystemEventHandler;
        _fileSystemWatcher.Created += FileSystemEventHandler;
        _fileSystemWatcher.Deleted += FileSystemEventHandler;
        _fileSystemWatcher.Error += ErrorEventHandler;
        _fileSystemWatcher.Renamed += RenamedEventHandler;
        _serverTimer = new Timer(_consolidationInterval);
        _serverTimer.Elapsed += ElapsedEventHandler;
        _serverTimer.AutoReset = true;
        _serverTimer.Enabled = _fileSystemWatcher.EnableRaisingEvents;
    }

    private void Uninitialize()
    {
        _fileSystemWatcher.Dispose();
        _serverTimer.Dispose();
    }

    private void FileSystemEventHandler(object sender, FileSystemEventArgs e)
    {
        _events.Add(new DelayedEvent(e));
    }

    private void ErrorEventHandler(object sender, ErrorEventArgs e)
    {
        OnError(e);
    }

    private void RenamedEventHandler(object sender, RenamedEventArgs e)
    {
        _events.Add(new DelayedEvent(e));
    }

    private void ElapsedEventHandler(object? sender, ElapsedEventArgs e)
    {
        // We don't fire the events inside the lock. We will queue them here until
        // the code exits the locks.
        Queue<DelayedEvent>? eventsToBeFired = null;
        if (_lock.TryEnter())
        {
            // Only one thread at a time is processing the events
            try
            {
                eventsToBeFired = new Queue<DelayedEvent>(32);
                // Lock the collection while processing the events
                lock (_events.SyncRoot)
                {
                    for (int i = 0; i < _events.Count; i++)
                    {
                        var current = (DelayedEvent)_events[i]!;
                        if (current.Delayed)
                        {
                            // This event has been delayed already so we can fire it
                            // We just need to remove any duplicates
                            for (int j = i + 1; j < _events.Count; j++)
                            {
                                if (current.IsDuplicate(_events[j]))
                                {
                                    // Removing later duplicates
                                    _events.RemoveAt(j);
                                    j--; // Don't skip next event
                                }
                            }

                            bool raiseEvent = true;
                            if (
                                current.Args.ChangeType is WatcherChangeTypes.Created or WatcherChangeTypes.Changed
                                && File.Exists(current.Args.FullPath)
                            )
                            {
                                //check if the file has been completely copied (can be opened for read)
                                FileStream? stream = null;
                                try
                                {
                                    stream = File.Open(
                                        current.Args.FullPath,
                                        FileMode.Open,
                                        FileAccess.Read,
                                        FileShare.None
                                    );
                                    // If this succeeds, the file is finished
                                }
                                catch (IOException)
                                {
                                    raiseEvent = false;
                                }
                                finally
                                {
                                    stream?.Close();
                                }
                            }

                            if (raiseEvent)
                            {
                                // Add the event to the list of events to be fired
                                eventsToBeFired.Enqueue(current);
                                // Remove it from the current list
                                _events.RemoveAt(i);
                                i--; // Don't skip next event
                            }
                        }
                        else
                        {
                            // This event was not delayed yet, so we will delay processing
                            // this event for at least one timer interval
                            current.Delayed = true;
                        }
                    }
                }
            }
            finally
            {
                _lock.Exit();
            }
        }
        // else - this timer event was skipped, processing will happen during the next timer event

        // Now fire all the events if any events are in eventsToBeFired
        RaiseEvents(eventsToBeFired);
    }

    public int ConsolidationInterval
    {
        get => _consolidationInterval;
        set
        {
            _consolidationInterval = value;
            _serverTimer.Interval = value;
        }
    }

    private void RaiseEvents(Queue<DelayedEvent>? deQueue)
    {
        if (deQueue is null)
        {
            return;
        }

        if (deQueue.Count == 0)
        {
            return;
        }

        while (deQueue.Count > 0)
        {
            var de = deQueue.Dequeue();
            switch (de.Args.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    OnChanged(de.Args);
                    break;
                case WatcherChangeTypes.Created:
                    OnCreated(de.Args);
                    break;
                case WatcherChangeTypes.Deleted:
                    OnDeleted(de.Args);
                    break;
                case WatcherChangeTypes.Renamed:
                    OnRenamed((RenamedEventArgs)de.Args);
                    break;
            }
        }
    }
    #endregion

    /// <summary>
    /// This class wraps FileSystemEventArgs and RenamedEventArgs objects and detection of duplicate events.
    /// </summary>
    private sealed class DelayedEvent
    {
        private readonly FileSystemEventArgs _args;

        public DelayedEvent(FileSystemEventArgs args)
        {
            Delayed = false;
            _args = args;
        }

        public FileSystemEventArgs Args => _args;

        /// <summary>
        /// Only delayed events that are unique will be fired.
        /// </summary>
        public bool Delayed { get; set; }

        public bool IsDuplicate(object? obj)
        {
            if (obj is not DelayedEvent delayedEvent)
            {
                return false; // this is not null so they are different
            }

            var eO1 = _args;
            var reO1 = _args as RenamedEventArgs;
            var eO2 = delayedEvent._args;
            var reO2 = delayedEvent._args as RenamedEventArgs;
            // The events are equal only if they are of the same type (reO1 and reO2
            // are both null or NOT NULL) and have all properties equal.
            // We also eliminate Changed events that follow recent Created events
            // because many apps create new files by creating an empty file and then
            // they update the file with the file content.
            return (
                    eO1.ChangeType == eO2.ChangeType
                    && eO1.FullPath == eO2.FullPath
                    && eO1.Name == eO2.Name
                    && (
                        (reO1 is null && reO2 is null)
                        || (
                            reO1 is not null
                            && reO2 is not null
                            && reO1.OldFullPath == reO2.OldFullPath
                            && reO1.OldName == reO2.OldName
                        )
                    )
                )
                || (
                    eO1.ChangeType == WatcherChangeTypes.Created
                    && eO2.ChangeType == WatcherChangeTypes.Changed
                    && eO1.FullPath == eO2.FullPath
                    && eO1.Name == eO2.Name
                );
        }
    }
}
