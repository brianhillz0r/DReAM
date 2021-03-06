/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace MindTouch.Data {

    /// <summary>
    /// Markup attribute for defining entity/database column mappings
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DataColumnAttribute : Attribute {

        //--- Fields ---

        /// <summary>
        /// Database column name for entity field.
        /// </summary>
        public string Name;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public DataColumnAttribute() { }

        /// <summary>
        /// Createa a new instance.
        /// </summary>
        /// <param name="name">Initial field name value.</param>
        public DataColumnAttribute(string name) {
            this.Name = name;
        }
    }

    /// <summary>
    /// This attribute is used to mark the class that is responsible to update the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DataUpgradeAttribute : Attribute {
    }

    /// <summary>
    /// This attribute is used to mark methods that update the database to a specific version
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EffectiveVersionAttribute : Attribute {

        //--- Fields ---
        private string _version;

        //--- Constructors ---
        public EffectiveVersionAttribute(string version) {
            _version = version;
        }

        //--- Methods ---
        public string VersionString {
            get { return _version; }
        }
    }

    /// <summary>
    /// This attribute is used to mark methods that check the integrity of data
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DataIntegrityCheck : Attribute {

        //--- Fields ---
        private string _version;

        //--- Constructors ---
        public DataIntegrityCheck(string version) {
            _version = version;
        }

        //--- Methods ---
        public string VersionString {
            get { return _version; }
        }
    }
}
