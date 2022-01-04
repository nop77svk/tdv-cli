# Tibco Data Virtualization CLI Client

A command-line interface client to Tibco Data Virtualization server since Tibco (says) does not have anything similar.

Run `tdvcli --help` to get the overview on available command line parameters.

## "Pierre's Tibco SQL" Script -- Server-side commands

### Create folder

`create [if not exists] folder `<_resource path_>`;`

The `if not exists` option instructs the server to not return error when such a folder/container already exists on the sever.

### Create schema

`create [if not exists] schema `<_resource path_>`;`

The `if not exists` option instructs the server to not return error when such a schema already exists on the sever.

### Create data view

`create [if not exists] view `<_resource path_>` as `<_query_>`;`

The `if not exists` option instructs the server to not return error when such a view already exists on the sever.

### Drop object(s)

`drop [if exists] `<_comma-delimited list of resource specifiers_>`;`

The `if exists` option instructs the server to not return error when any of the objects listed do not exist on the server.

### Drop objects' contents

`purge [if exists] `<_comma-delimited list of resource specifiers_>`;`

The `if exists` option instructs the server to not return error when any of the objects listed do not exist on the server.

This almost has the same effect as `drop...`, except for keeping the listed resource in place, not deleted.

### Grant privileges

`grant `<_modus operandi_>` [recursive] `<_comma-delimited list of privileges_>` on `<_comma-delimited list of resource specifiers_>` to `<_comma-delimited list of liberal principals>`;`

The command grants privileges <_comma-delimited list of privileges_> to resources <_comma-delimited list of resource specifiers_> to grantees/principals <_comma-delimited list of principals>.

<_modus operandi_> of `set` causes the server to replace all privileges already assigned to the specified resources with the specified privileges, whereas `append` causes the server to append the specfied privileges to the privileges already assigned to the specified resources.

`recursive` option is valid for data sources, catalogs, schemas and folders only and makes the grant statement operate on the whole resource tree. The option is ignored for other resource types.

Available privileges are
* `read`,
* `write`,
* `execute`,
* `select`,
* `insert`,
* `update`,
* `delete`,
* `grant`.

Resource specifiers are described in their own section.

Liberal/strict principal specifiers are described in their own section.

### RBS/RLS policy assignment and removal

`<assign|unassign> <rbs|rls> pol[icy] `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) row-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

### CBS/CLS policy assignment and removal

`<assign|unassign> <cbs|cls> pol[icy] <func[tion]|proc[edure]> `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) column-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

**Note:** Implementation pending!

### Mass-publishing

`publish [if not exists] `<_source resource path_>` to `<_target resource path_>` [flatten hierarchy with `<_string literal_>`];`

If the source resource specified is a table/view or a stored procedure, then it gets published under the target path under the same name. The `flatten hierarchy` option is invalid/forbidden in this case.

If the source resource specified is a folder, then all of its contents (recursively) get published under the target path, with all relative subpaths flattened to a single level (schema) by the hierarchy flattening string. All individual objects (tables, views, stored procedures) are left their names intact.

**Example:** Consider the hierarchy of views as follows
* `/shared/L1_Physical/source/customers1`,
* `/shared/L1_Physical/source/items1`,
* `/shared/L1_Physical/source/customers_items_j1`,
* `/shared/L1_Physical/DWH/Oracle/customers2`,
* `/shared/L1_Physical/DWH/Oracle/items2`,
* `/shared/L1_Physical/DWH/Oracle/customers_items_j2`,
* `/shared/L1_Physical/DWH/PgSQL/customers3`,
* `/shared/L1_Physical/DWH/PgSQL/items3`,
* `/shared/L1_Physical/DWH/PgSQL/customers_items_j3`,
* `/shared/L1_Physical/DWH/BigData/Impala/customers4`,
* `/shared/L1_Physical/DWH/BigData/Impala/items4`,
* `/shared/L1_Physical/DWH/BigData/Impala/customers_items_j4`,
* `/shared/L1_Physical/DWH/BigData/Hive/customers5`,
* `/shared/L1_Physical/DWH/BigData/Hive/items5`,
* `/shared/L1_Physical/DWH/BigData/Hive/customers_items_j5`.

Executing `publish /shared/L1_Physical to /services/databases/PublishTest flatten hierarchy with "___";` will result in the `PublishTest` published data source with schemas

* `source`,
* `DWH___Oracle`,
* `DWH___PgSQL`,
* `DWH___BigData___Impala`,
* `DWH___BigData___Hive`,

containing published views as follows

* `source/customers1`,
* `source/items1`,
* `source/customers_items_j1`,
* `DWH___Oracle/customers2`,
* `DWH___Oracle/items2`,
* `DWH___Oracle/customers_items_j2`,
* `DWH___PgSQL/customers3`,
* `DWH___PgSQL/items3`,
* `DWH___PgSQL/customers_items_j3`,
* `DWH___BigData___Impala/customers4`,
* `DWH___BigData___Impala/items4`,
* `DWH___BigData___Impala/customers_items_j4`,
* `DWH___BigData___Hive/customers5`,
* `DWH___BigData___Hive/items5`,
* `DWH___BigData___Hive/customers_items_j5`.

### Object/resource description

`desc[ribe] `<_list of resource paths_>`;`

The command displays info on the resources specified by the supplied resource paths. Here, full resource specifiers are not necessary, since the `describe` statement is intended for retrieving the actual resource type information of unknown TDV resources.

## "Pierre's Tibco SQL" Script -- Common clauses

### Resource specifier

<_resource specifier_> = <_resource type_> <_resource path_>

<_resource type_> is one of the Tibco DV resource types:
* `container`,
* `table`,
* `trigger`,
* `datasource`, `data_source`, `data source`,
* `procedure`,
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
