/**
 * Shared builder used by both `ROUTES` (page routes) and `API_ROUTES` (API endpoints).
 *
 * `route()` turns a path pattern into a function that fills in each `:param`, while keeping the
 * original pattern available on `.pattern` for `<Route path={...} />` definitions:
 *
 *   const view = route('/admin/la-users/:userId');
 *   view.pattern  // '/admin/la-users/:userId'
 *   view('123')   // '/admin/la-users/123'
 *
 * Arguments are positional and follow the order the parameters appear in the pattern. The argument
 * count is checked at compile time.
 */

/**
 * Anything a route parameter can be. Deliberately as permissive as the template literals these
 * builders replace — `useParams()` returns `string | undefined`, and values are stringified the
 * same way `${value}` used to stringify them. The useful check here is the *number* of arguments,
 * which the tuple type below enforces.
 */
export type RouteParam = string | number | null | undefined;

/** One positional argument per `:param` in the pattern. */
type PathParams<TPattern extends string> = TPattern extends `${string}:${string}/${infer TRest}`
  ? [RouteParam, ...PathParams<`/${TRest}`>]
  : TPattern extends `${string}:${string}`
  ? [RouteParam]
  : [];

export type RouteBuilder<TPattern extends string> = ((...params: PathParams<TPattern>) => string) & {
  readonly pattern: TPattern;
};

export const route = <TPattern extends string>(pattern: TPattern): RouteBuilder<TPattern> => {
  const build = (...params: PathParams<TPattern>): string => {
    const values = params as readonly RouteParam[];
    let index = 0;
    return pattern.replace(/:[^/]+/g, () => String(values[index++]));
  };

  return Object.assign(build, { pattern } as const);
};
