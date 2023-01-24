﻿using System.CommandLine;

namespace kix.Commands.Database;

internal abstract class DatabaseCommandBase : Command
{
    protected Option<string> DatabaseOption;

    protected Option<string> ToolOption;

    protected Option<string> GroupOption;

    protected Option<string> IdOption;

    protected Option<string> ToolLikeOption;

    protected Option<string> GroupLikeOption;

    protected Option<string> IdLikeOption;

    protected Option<string> NameLikeOption;

    protected Option<bool> ListResourceOption;

    protected Option<bool> DetailedOption;


    protected DatabaseCommandBase(string name, string? description = null) : base(name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file.");
        DatabaseOption.ArgumentHelpName = "file";
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        ToolOption = new Option<string>(new[] { "-t", "--tool" }, "Tool to filter by.");
        ToolOption.ArgumentHelpName = "value";
        AddOption(ToolOption);
        GroupOption = new Option<string>(new[] { "-g", "--group" }, "Group to filter by.");
        GroupOption.ArgumentHelpName = "value";
        AddOption(GroupOption);
        IdOption = new Option<string>(new[] { "-i", "--id" }, "Id to filter by.");
        IdOption.ArgumentHelpName = "value";
        AddOption(IdOption);
        ToolLikeOption = new Option<string>(new[] { "--tool-like" }, "Tool pattern to filter by.");
        ToolLikeOption.ArgumentHelpName = "pattern";
        AddOption(ToolLikeOption);
        GroupLikeOption = new Option<string>(new[] { "--group-like" }, "Group pattern to filter by.");
        GroupLikeOption.ArgumentHelpName = "pattern";
        AddOption(GroupLikeOption);
        IdLikeOption = new Option<string>(new[] { "--id-like" }, "Id pattern to filter by.");
        IdLikeOption.ArgumentHelpName = "pattern";
        AddOption(IdLikeOption);
        NameLikeOption = new Option<string>(new[] { "--name-like" }, "Name pattern to filter by.");
        NameLikeOption.ArgumentHelpName = "pattern";
        AddOption(NameLikeOption);
        ListResourceOption = new Option<bool>(new[] { "-l", "--list-resource" }, "List resource items.");
        AddOption(ListResourceOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries.");
        AddOption(DetailedOption);
    }
}
