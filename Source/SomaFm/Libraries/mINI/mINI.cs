namespace mINI
{
	public abstract class INIReader
	{
		/// <summary>
		///    Called when the current line is empty.
		/// </summary>
		protected virtual void OnEmpty()
		{
		}

		/// <summary>
		///    Called when the current line is a comment.
		/// </summary>
		/// <param name="text">Comment text, prefix (; or #) included.</param>
		protected virtual void OnComment(string text)
		{
		}

		/// <summary>
		///    Called when the current line is a section,
		///    before reading subsections.
		/// </summary>
		/// <param name="section">
		///    Complete section name, regardless of subsections
		///    and inner whitespace. Example: "a/b /c/  d".
		/// </param>
		protected virtual void OnSection(string section)
		{
		}

		/// <summary>
		///    Called when a section name is empty, not including subsections.
		///    This method is called before calling OnSection.
		/// </summary>
		protected virtual void OnSectionEmpty()
		{
		}

		/// <summary>
		///    Called each time a subsection is found in a section line.
		///    <para>
		///       Example: for a line such as: [a/b/c], this method
		///       is called 3 times with the following arguments:
		///    </para>
		///    <para> OnSubSection("a", "a") </para>
		///    <para> OnSubSection("b", "a/b") </para>
		///    <para> OnSubSection("c", "a/b/c") </para>
		/// </summary>
		/// <param name="subsection">Subsection name.</param>
		/// <param name="path">Subsection path, including parents.</param>
		protected virtual void OnSubSection(string subsection, string path)
		{
		}

		/// <summary>
		///    Called when a subsection name is empty.
		///    This method is called before calling OnSubSection.
		/// </summary>
		/// <param name="path">Subsection path, including parents.</param>
		protected virtual void OnSubSectionEmpty(string path)
		{
		}

		/// <summary>
		///    Called when the current line is a key=value pair.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value associated with the key.</param>
		protected virtual void OnKeyValue(string key, string value)
		{
		}

		/// <summary>
		///    Called when the key is empty in a key=value pair.
		///    This method is called before calling OnKeyValue.
		/// </summary>
		/// <param name="value">Value associated with the key.</param>
		protected virtual void OnKeyEmpty(string value)
		{
		}

		/// <summary>
		///    Called when the value is empty in a key=value pair.
		///    This method is called before calling OnKeyValue.
		/// </summary>
		/// <param name="key">Key specified for the value.</param>
		protected virtual void OnValueEmpty(string key)
		{
		}

		/// <summary>
		///    Called when the reader is unable to read the current line.
		/// </summary>
		/// <param name="line">Complete input line, not trimmed.</param>
		protected virtual void OnUnknown(string line)
		{
		}

		/// <summary>
		///    Try to read an empty line.
		/// </summary>
		/// <param name="line">Input line, trimmed.</param>
		private bool ReadEmpty(string line)
		{
			if (line != string.Empty)
				return false;

			OnEmpty();
			return true;
		}

		/// <summary>
		///    Try to read a comment.
		/// </summary>
		/// <param name="line">Input line, trimmed.</param>
		private bool ReadComment(string line)
		{
			if (!(line.StartsWith("#") || line.StartsWith(";")))
				return false;

			OnComment(line);
			return true;
		}

		/// <summary>
		///    Try to read a (possibly nested) section.
		/// </summary>
		/// <param name="line">Input line, trimmed.</param>
		private bool ReadSection(string line)
		{
			if (!(line.StartsWith("[") && line.EndsWith("]")))
				return false;

			var section = line.Substring(1, line.Length - 2).Trim();

			if (section == string.Empty)
				OnSectionEmpty();

			OnSection(section);
			ReadSubSections(section);
			return true;
		}

		/// <summary>
		///    Read subsections in a given section.
		/// </summary>
		/// <param name="section">Section name, trimmed.</param>
		private void ReadSubSections(string section)
		{
			var subsections = section.Split('/');

			// first subsection is special, no separator, name/path identical:
			var path = subsections[0].Trim();

			if (path == string.Empty)
				OnSubSectionEmpty(path);

			OnSubSection(path, path);

			// accumulate path:
			for (var i = 1; i < subsections.Length; i++)
			{
				var subsection = subsections[i].Trim();
				path += "/" + subsection;

				if (subsection == string.Empty)
					OnSubSectionEmpty(path);

				OnSubSection(subsection, path);
			}
		}

		/// <summary>
		///    Try to read a key=value pair.
		/// </summary>
		/// <param name="line">Input line, trimmed.</param>
		private bool ReadKeyValue(string line)
		{
			if (!line.Contains("="))
				return false;

			var pair = line.Split(new[] {'='}, 2);
			var key = pair[0].Trim();
			var value = pair[1].Trim();

			if (key == string.Empty)
				OnKeyEmpty(value);

			if (value == string.Empty)
				OnValueEmpty(key);

			OnKeyValue(key, value);
			return true;
		}

		/// <summary>
		///    Read an INI line.
		/// </summary>
		/// <param name="line">Input line.</param>
		public void ReadLine(string line)
		{
			var trimmedLine = line.Trim();

			if (ReadEmpty(trimmedLine)
			    || ReadComment(trimmedLine)
			    || ReadSection(trimmedLine)
			    || ReadKeyValue(trimmedLine))
				return;

			// not trimmed:
			OnUnknown(line);
		}
	}
}