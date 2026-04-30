using LmpClient.Base.Interface;
using LmpClient.Events;
using LmpCommon.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable ForCanBeConvertedToForeach

namespace LmpClient.Base
{
    /// <inheritdoc cref="SystemBase" />
    /// <inheritdoc cref="ISystem" />
    /// <summary>
    /// System base class. This class is made for a grouping logic.
    /// </summary>
    public abstract class System<T> : SystemBase, ISystem
        where T : ISystem, new()
    {
        #region Field & Properties

        public static T Singleton { get; } = new T();

        public abstract string SystemName { get; }

        public virtual int ExecutionOrder { get; } = 0;

        private List<RoutineDefinition> FixedUpdateRoutines { get; } = new List<RoutineDefinition>();
        private List<RoutineDefinition> UpdateRoutines { get; } = new List<RoutineDefinition>();
        private List<RoutineDefinition> LateUpdateRoutines { get; } = new List<RoutineDefinition>();

        #endregion

        protected System() => NetworkEvent.onNetworkStatusChanged.Add(NetworkEventHandler);

        protected virtual ClientState EnableStage => ClientState.Running;

        protected virtual void NetworkEventHandler(ClientState data)
        {
            if (data <= ClientState.Disconnected)
            {
                Enabled = false;
            }

            if (data == EnableStage)
            {
                Enabled = true;
            }
        }

        protected virtual bool AlwaysEnabled { get; } = false;

        protected void SetupRoutine(RoutineDefinition routine)
        {
            if (routine == null)
            {
                LunaLog.LogError("[LMP]: Cannot set a null routine!");
                return;
            }

            if (routine.Execution == RoutineExecution.FixedUpdate && FixedUpdateRoutines.All(r => r.Name != routine.Name))
            {
                FixedUpdateRoutines.Add(routine);
            }
            else if (routine.Execution == RoutineExecution.Update && UpdateRoutines.All(r => r.Name != routine.Name))
            {
                UpdateRoutines.Add(routine);
            }
            else if (routine.Execution == RoutineExecution.LateUpdate && LateUpdateRoutines.All(r => r.Name != routine.Name))
            {
                LateUpdateRoutines.Add(routine);
            }
            else
            {
                LunaLog.LogError($"[LMP]: Routine {routine.Name} already defined");
            }
        }

        protected void ChangeRoutineExecutionInterval(RoutineExecution execution, string routineName, int newIntervalInMs)
        {
            RoutineDefinition routine;
            switch (execution)
            {
                case RoutineExecution.FixedUpdate:
                    routine = FixedUpdateRoutines.FirstOrDefault(r => r.Name == routineName);
                    break;
                case RoutineExecution.Update:
                    routine = UpdateRoutines.FirstOrDefault(r => r.Name == routineName);
                    break;
                case RoutineExecution.LateUpdate:
                    routine = LateUpdateRoutines.FirstOrDefault(r => r.Name == routineName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(execution), execution, null);
            }

            if (routine != null)
            {
                routine.IntervalInMs = newIntervalInMs;
            }
            else
            {
                LunaLog.LogError($"[LMP]: Routine {execution}/{routineName} not defined");
            }
        }

        private bool _enabled;

        public virtual bool Enabled
        {
            get => AlwaysEnabled || _enabled;
            set
            {
                if (!AlwaysEnabled)
                {
                    if (!_enabled && value)
                    {
                        _enabled = true;
                        OnEnabled();
                    }
                    else if (_enabled && !value)
                    {
                        _enabled = false;
                        OnDisabled();
                        RemoveRoutines();
                    }
                }
            }
        }

        protected virtual void OnEnabled()
        {
        }

        protected virtual void OnDisabled()
        {
        }

        protected virtual void RemoveRoutines()
        {
            UpdateRoutines.Clear();
            FixedUpdateRoutines.Clear();
            LateUpdateRoutines.Clear();
        }

        public void FixedUpdate()
        {
            RunRoutines(FixedUpdateRoutines, nameof(FixedUpdate));
        }

        public void Update()
        {
            RunRoutines(UpdateRoutines, nameof(Update));
        }

        public void LateUpdate()
        {
            RunRoutines(LateUpdateRoutines, nameof(LateUpdate));
        }

        private void RunRoutines(List<RoutineDefinition> routines, string stageName)
        {
            for (var i = 0; i < routines.Count; i++)
            {
                try
                {
                    routines[i]?.RunRoutine();
                }
                catch (Exception e)
                {
                    LunaLog.LogError($"[LMP]: Error running {SystemName}/{stageName} routine '{routines[i]?.Name}': {e}");
                }
            }
        }
    }
}