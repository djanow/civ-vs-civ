using System;
using System.Collections.Generic;

namespace CivVSCiv
{
    /// <summary>
    /// Bus d'événements global. Chaque système s'abonne aux événements
    /// qui le concernent et publie ceux qu'il produit.
    /// Thread-safe pour le thread principal Unity uniquement.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
            {
                _handlers[type] = Delegate.Combine(_handlers[type], handler);
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type)) return;

            _handlers[type] = Delegate.Remove(_handlers[type], handler);
            if (_handlers[type] == null)
            {
                _handlers.Remove(type);
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var del) && del is Action<T> action)
            {
                action.Invoke(eventData);
            }
        }

        /// <summary>
        /// Vide tous les handlers. Appelé au chargement d'une nouvelle partie.
        /// </summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
