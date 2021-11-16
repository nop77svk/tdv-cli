# Tibco Data Virtualization CLI Client

A command-line interface client to Tibco Data Virtualization server since Tibco (says) does not have anything similar.

Run `tdvcli --help` to get the overview on available command line parameters.

## "Pierre's Tibco SQL" Script -- Server-side commands

### Create folder

`create [if not exists] <folder|container> `<_resource path_>`;`

The `if not exists` option instructs the server to not return error when such a folder/container already exists on the sever.

Note that `folder` and `container` keywords are interchangeable.

### Create data view

`create [if not exists] view `<_resource path_>` as `<_query_>`;`

The `if not exists` option instructs the server to not return error when such a view already exists on the sever.

**Note:** Implementation pending!

### Drop object(s)

`drop [recursive] [if exists] `<_comma-delimited list of resource specifiers_>`;`

The `recursive` option is valid for data sources, catalogs, schemas and folders only; makes the drop statement drop the whole object tree.

The `if exists` option instructs the server to not return error when any of the objects listed do not exist on the server.

### Grant privileges

`grant `<_modus operandi_>` [recursive] `<_comma-delimited list of privileges_>` on `<_comma-delimited list of resource specifiers_>` to `<_comma-delimited list of liberal principals>`;`

The command grants privileges <_comma-delimited list of privileges_> to resources <_comma-delimited list of resource specifiers_> to grantees/principals <_comma-delimited list of principals>.

<_modus operandi_> of `set` causes the server to replace all privileges already assigned to the specified resources with the specified privileges, whereas `append` causes the server to append the specfied privileges to the privileges already assigned to the specified resources.

`recursive` option is valid for data sources, catalogs, schemas and folders only and makes the grant statement operate on the whole resource tree. The option is ignored for other resource types.

Available privileges are
* `read`,
* `select`,
* `insert`,
* `update`,
* `delete`,
* `grant`.

Resource specifiers are described in their own section.

Liberal/strict principal specifiers are described in their own section.

### RBS/RLS policy assignment and removal

`<assign|unassign> <rbs|rls> pol[icy] <func[tion]|proc[edure]> `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) row-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

### CBS/CLS policy assignment and removal

`<assign|unassign> <cbs|cls> pol[icy] <func[tion]|proc[edure]> `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) column-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

**Note:** Implementation pending!

### Object/resource description

`desc[ribe] `<_list of resource paths_>`;`

The command displays info on the resources specified by the supplied resource paths. Here, full resource specifiers are not necessary, since the `describe` statement is intended for retrieving the actual resource type information of unknown TDV resources.

## "Pierre's Tibco SQL" Script -- Common clauses

### Resource specifier

<_resource specifier_> = <_resource type_> <_resource path_>

<_resource type_> is one of the Tibco DV resource types:
* `container`, `folder`,
* `table`, `view`,
* `trigger`,
* `datasource`, `data_source`, `data source`,
* `procedure`, `function`,
* `link`,
* `definitionset`, `definition_set`, `definition set`,
* `adapter`,
* `extension`,
* `model`,
* `policy`,
* `relationship`,
* `tree`.

<_resource path_> is a path to a resource starting with `/`, with levels of hierarchy delimited by `/`.

#### Example of a list of resource specifiers

```
container /shared/L1_Physical,
    container /shared/L3_Application,
    data source /services/databases/MyPublishedDB,
    policy /shared/MyRBSPolicyFunction
```

### Principal specifier -- Compact

<_principal specifier_> = <_principal type_> <_principal_name>`@`<_principal domain_>

<_principal type_> is one of
* `user`, or
* `group`.

<_principal name_> and <_principal domain_> is a valid user/group name and domain name, resp.

#### Examples

```
user jose@composite, user maria@composite, group administrators@composite, group all@dynamic
```

### Principal specifier -- Verbose/strict

<_principal specifier_> = <_principal domain_> <_principal type_>` equal [to] `<_principal name_>

<_principal type_> is one of
* `user`, or
* `group`.

<_principal name_> and <_principal domain_> is a valid user/group name and domain name, resp.

#### Examples

```
composite user jose, composite user equal to maria, composite group administrators, dynamic group equal to all
```

### Principal specifier -- Verbose/liberal

<_principal specifier_> = <_principal domain_> <_principal type_>` [rlike|rxlike|regexlike|regexplike|matching] `<_regular expression_>`;`

<_principal type_> is one of
* `user`, or
* `group`.

<_principal domain_> is a valid domain name.

<_regular expression_> is a regular expression against which all available <_principal domain_> users/groups will be matched (case-insensitively). The regular expression must be enclosed between `/` and `/` characters.

#### Examples

* `composite user rlike /^jose_/` will match all user names starting with `jose_` from domain `composite`,
* `composite user rxlike /_the_mighty$/` will match all user names ending with `_the_mighty` from domain `composite`,
* `composite group matching /^ro_.*_\d+$/` will match all group names starting with `ro_` and ending with a nonnegative integer number from domain `composite`.

## "Pierre's Tibco SQL" Script -- Client-side directives and commands

### Prompt

`#prompt `<_character string literal_>`;`

Displays the <_character string literal_> to the TDV CLI's output.

The <_character string literal_> is either `"`-enclosed or `'`-enclosed sequence of characters. Please note that there's no `"`/`'` escaping implemented.
