# Tibco Data Virtualization CLI Client

A command-line interface client to Tibco Data Virtualization server since Tibco (say they) do not have anything similar.

Run `tdvcli --help` to get the overview on available command line parameters.

Project is frozen FTTB. If you need any further functionality, feel free to contact me or fork it and modify it for yourself.

<a rel="license" href="http://creativecommons.org/licenses/by-sa/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by-sa/4.0/88x31.png" /></a><br />This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-sa/4.0/">Creative Commons Attribution-ShareAlike 4.0 International License</a>.

## "Pierre's Tibco SQL" Script -- Server-side commands

### Introspection

`introspect ` <_multi-datasource clause_> [<_handling of introspectables_>] `;`

<_multi-datasource clause_> = comma-delimited list of 1 or more of <_data source clause_>

<_data source clause_> = `data source `<_data source path_> [<_multi-catalog subclause_>]

<_handling of introspectables_>\
    = ( `drop` | `keep` ) ` unmatched and ` ( `skip` | `update` ) ` existing resources`\
    | ( `skip` | `update` ) ` existing resources`\
    | ( `drop` | `keep` ) ` unmatched existing resources`

<_multi-catalog subclause_> = `(` comma-delimited list of 1 or more of <_catalog specifier_> `)`

<_catalog specifier_> = `catalog ` <_liberal resource identifier_> [<_multi-schema subclause_>]

<_multi-schema subclause_> = `(` comma-delimited list of 1 or more of <_schema clause_> `)`

<_schema clause_> = `schema ` <_liberal resource identifier_> [<_multi-object subclause_>]

<_multi-object subclause_> = `(` comma-delimited list of 1 or more of <_object specifier_> `)`

<_object specifier_> = <_object operation_> ` ` <_liberal resource identifier_>

<_object operation_> = `include` | `exclude`

<_liberal resource identifier_> = <_regexp identifier matching_> | <_exact identifier matching_>

<_exact identifier matching_> = [`equal to `] <_any valid resource identifier_>

<_regexp identifier matching_> = ( `matching ` | `rlike ` | `rxlike ` | `regexlike ` | `regexplike ` ) `/` <_[regular expression in .NET 6 syntax](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference)_> `/`

Run introspection on listed data sources. Each data source listed must exist. At least one data source must be specified.

Optionally, for each data source you may restrict catalogs to be introspected. The catalogs listed are matched against real introspectables as retrieved from the remote data source. Any unmatched catalogs are silently ignored.

Optionally, for each catalog you may restrict schemas to be introspected. The schemas listed are matched against real introspectables as retrieved from the remote data source and matched catalog. Any unmatched schemas are silently ignored.

Optionally, for each schema you may restrict objects(tables) to be introspected. The objects listed are matched against real introspectables as retrieved from the remote data source and matched catalog and schema. Restriction of objects(tables) is done via inclusions and exclusions in an ordered manner from first to last, i.e. if a table matches an exclusion specifier, but matches a later inclusion specifier, the table gets introspected.

If an object restriction list starts with `exclude`, then all introspectables retrieved from remote data sources are implicitly considered included for introspection at the start, i.e., as if the object restriction list started with (an invisible) `include matching /^/`.

Vice versa, if an object restriction list starts with `include`, then all introspectables retrieved from remote data sources are implicitly considered excluded for introspection at the start, i.e., as if the object restriction list started with (an invisible) `exclude matching /^/`.

#### Handling of introspectables (optional clause)

`drop unmatched resources` causes the introspection to remove all existing objects(tables), schemas and catalogs under each listed data source that are not present on the remote data source anymore.

Vice versa, `keep unmatched resources` causes the introspection to keep all existing objects(tables), schemas and catalogs under each listed data source, even if they are not present on the remote data source.

`update existing resources` causes the introspection to re-introspect all existing objects(tables) under each listed data source.

Vice versa, `skip existing resources` causes the introspection to ignore/skip re-introspecting existing objects(tables) under each listed data source.

### Create folder

`create `[`if not exists`]` folder `<_resource path_>`;`

The `if not exists` option instructs the server to not return error when such a folder/container already exists on the sever.

### Create schema

`create `[`if not exists`]` schema `<_resource path_>`;`

The `if not exists` option instructs the server to not return error when such a schema already exists on the sever.

### Create data view

`create `[`if not exists`]` view `<_resource path_>` as `<_query_>`;`

The `if not exists` option instructs the server to not return error when such a view already exists on the sever.

### Drop object(s)

`drop `[`if exists`]` `<_comma-delimited list of resource specifiers_>`;`

The `if exists` option instructs the server to not return error when any of the objects listed do not exist on the server.

### Drop objects' contents

`purge `[`if exists`]` `<_comma-delimited list of resource specifiers_>`;`

The `if exists` option instructs the server to not return error when any of the objects listed do not exist on the server.

This almost has the same effect as `drop...`, except for keeping the listed resource in place, not deleted.

### Grant privileges

`grant `<_modus operandi_>` `[`recursive`]` `<_comma-delimited list of privileges_>` on `<_comma-delimited list of resource specifiers_>` to `<_comma-delimited list of liberal principals_>` `[`propagate `<_propagation directions_>]`;`

The command grants privileges <_comma-delimited list of privileges_> to resources <_comma-delimited list of resource specifiers_> to grantees/principals <_comma-delimited list of principals_>.

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

Specify propagation directions
* `downstream`/`to consumers` to propagate the privileges to objects that depend on the granted objects,
* `upstream`/`to producers` to propagate the privileges to objects the granted objects depend on,
* `up and down`/`down and up`/`to consumers and to producers`/`to producers and to consumers`/`both directions`/... to propagate the privileges in both directions described above.

**Warning:** Recursive privilege assignment does not work with privilege propagation options. This is due to the design limitation (read: flaw) of Tibco's DV server as of version 8.4. Propagation only works when granting privileges on individual TDV resources.

### RBS/RLS policy assignment and removal

<`assign`|`unassign`>` `<`rbs`|`rls`>` pol`[`icy`]` `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) row-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

### CBS/CLS policy assignment and removal

<`assign`|`unassign`>` `<`cbs`|`cls`>` pol`[`icy`]` `<`func`[`tion`]|`proc`[`edure`]>` `<_policy function resource path_>` to `<_comma-delimited list of resource specifiers_>`;`

The command assigns (statement `assign`) or unassigns/removes (statement `unassign`) column-level security policy identified by the policy function/procedure <_policy function resource path_> to the specified resources and all their children resources recursively.

Resource specifiers are described in their own section.

**Note:** CLS assignment implementation pending!

### Mass-publishing

`publish `[`if not exists`]` `<_source resource path_>` to `<_target resource path_>` `[`flatten hierarchy with `<_string literal_>]`;`

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

`desc`[`ribe`]` `<_list of resource paths_>`;`

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

<_principal specifier_> = <_principal domain_> <_principal type_>` equal `[`to`]` `<_principal name_>

<_principal type_> is one of
* `user`, or
* `group`.

<_principal name_> and <_principal domain_> is a valid user/group name and domain name, resp.

#### Examples

```
composite user jose, composite user equal to maria, composite group administrators, dynamic group equal to all
```

### Principal specifier -- Verbose/liberal

<_principal specifier_> = <_principal domain_> <_principal type_>` `[`rlike|rxlike|regexlike|regexplike|matching`]` `<_[regular expression in .NET 6 syntax](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference)_>`;`

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
