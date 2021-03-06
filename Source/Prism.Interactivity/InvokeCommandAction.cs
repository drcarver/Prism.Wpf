// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Microsoft.Practices.Prism.Interactivity
{
    /// <summary>
    /// Trigger action that executes a command when invoked. 
    /// It also maintains the Enabled state of the target control based on the CanExecute method of the command.
    /// </summary>
    public class InvokeCommandAction : TriggerAction<UIElement>
    {
        /// <summary>
        /// Dependency property identifying the command to execute when invoked.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(InvokeCommandAction),
            new PropertyMetadata(null, (d, e) => ((InvokeCommandAction)d).OnCommandChanged((ICommand)e.NewValue)));

        /// <summary>
        /// Dependency property identifying the command parameter to supply on command execution.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter",
            typeof(object),
            typeof(InvokeCommandAction),
            new PropertyMetadata(null, (d, e) => ((InvokeCommandAction)d).OnCommandParameterChanged((object)e.NewValue)));

        /// <summary>
        /// Dependency property identifying the TriggerParameterPath to be parsed to identify the child property of the trigger parameter to be used as the command parameter.
        /// </summary>
        public static readonly DependencyProperty TriggerParameterPathProperty = DependencyProperty.Register(
            "TriggerParameterPath",
            typeof(string),
            typeof(InvokeCommandAction),
            new PropertyMetadata(null, (d, e) => {}));

        private ExecutableCommandBehavior commandBehavior;

        /// <summary>
        /// Gets or sets the command to execute when invoked.
        /// </summary>
        public ICommand Command
        {
            get { return this.GetValue(CommandProperty) as ICommand; }
            set { this.SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets the command parameter to supply on command execution.
        /// </summary>
        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the TriggerParameterPath value.
        /// </summary>
        public string TriggerParameterPath
        {
            get { return this.GetValue(TriggerParameterPathProperty) as string; }
            set { this.SetValue(TriggerParameterPathProperty, value); }
        }
       
        /// <summary>
        /// Public wrapper of the Invoke method.
        /// </summary>
        public void InvokeAction(object parameter)
        {
            this.Invoke(parameter);
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">This parameter is passed to the command; the CommandParameter specified in the CommandParameterProperty is used for command invocation if not null.</param>
        protected override void Invoke(object parameter)
        {
            if (!string.IsNullOrEmpty(TriggerParameterPath))
            {
                //Walk the ParameterPath for nested properties.
                var propertyPathParts = TriggerParameterPath.Split('.');
                object propertyValue = parameter;
                foreach (var propertyPathPart in propertyPathParts)
                {
                    var propInfo = propertyValue.GetType().GetTypeInfo().GetDeclaredProperty(propertyPathPart);
                    propertyValue = propInfo.GetValue(propertyValue);
                }
                parameter = propertyValue;
            }

            var behavior = this.GetOrCreateBehavior();

            if (behavior != null)
            {
                behavior.ExecuteCommand(parameter);
            }
        }

        /// <summary>
        /// Sets the Command and CommandParameter properties to null.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.Command = null;
            this.CommandParameter = null;

            this.commandBehavior = null;
        }

        /// <summary>
        /// This method is called after the behavior is attached.
        /// It updates the command behavior's Command and CommandParameter properties if necessary.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            // In case this action is attached to a target object after the Command and/or CommandParameter properties are set,
            // the command behavior would be created without a value for these properties.
            // To cover this scenario, the Command and CommandParameter properties of the behavior are updated here.
            var behavior = this.GetOrCreateBehavior();

            if (behavior.Command != this.Command)
            {
                behavior.Command = this.Command;
            }

            if (behavior.CommandParameter != this.CommandParameter)
            {
                behavior.CommandParameter = this.CommandParameter;
            }
        }

        private void OnCommandChanged(ICommand newValue)
        {
            var behavior = this.GetOrCreateBehavior();

            if (behavior != null)
            {
                behavior.Command = newValue;
            }
        }

        private void OnCommandParameterChanged(object newValue)
        {
            var behavior = this.GetOrCreateBehavior();

            if (behavior != null)
            {
                behavior.CommandParameter = newValue;
            }
        }

        private ExecutableCommandBehavior GetOrCreateBehavior()
        {
            // In case this method is called prior to this action being attached, 
            // the CommandBehavior would always keep a null target object (which isn't changeable afterwards).
            // Therefore, in that case the behavior shouldn't be created and this method should return null.
            if (this.commandBehavior == null && this.AssociatedObject != null)
            {
                this.commandBehavior = new ExecutableCommandBehavior(this.AssociatedObject);
            }

            return this.commandBehavior;
        }

        /// <summary>
        /// A CommandBehavior that exposes a public ExecuteCommand method. It provides the functionality to invoke commands and update Enabled state of the target control.
        /// It is not possible to make the <see cref="InvokeCommandAction"/> inherit from <see cref="CommandBehaviorBase{T}"/>, since the <see cref="InvokeCommandAction"/>
        /// must already inherit from <see cref="TriggerAction{T}"/>, so we chose to follow the aggregation approach.
        /// </summary>
        private class ExecutableCommandBehavior : CommandBehaviorBase<UIElement>
        {
            /// <summary>
            /// Constructor specifying the target object.
            /// </summary>
            /// <param name="target">The target object the behavior is attached to.</param>
            public ExecutableCommandBehavior(UIElement target)
                : base(target)
            {
            }

            /// <summary>
            /// Executes the command, if it's set.
            /// </summary>
            public new void ExecuteCommand(object parameter)
            {
                base.ExecuteCommand(parameter);
            }
        }
    }
}