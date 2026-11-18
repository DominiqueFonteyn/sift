import "@typespec/http";

using Http;

namespace Sift;

@doc("The resume text to save. The signed-in user's single resume is created or replaced.")
model SaveResumeRequest {
  @doc("Plain-text resume. Must contain at least one non-whitespace character; blank or whitespace-only text is rejected with 400. At most 20,000 characters, counted as sent in UTF-16 code units (JavaScript string length), with no trimming before counting.")
  @minLength(1)
  @maxLength(20000)
  @pattern("\\S")
  text: string;
}

@doc("The signed-in user's resume.")
model Resume {
  @doc("Plain-text resume, exactly as saved.")
  text: string;

  @doc("When the resume was last saved, in UTC, taken from the application clock.")
  updatedAt: utcDateTime;
}

@doc("The signed-in user has no resume yet.")
@error
model ResumeNotFoundProblem {
  @statusCode _: 404;
  ...ProblemDetails;
}

@route("/resume")
@tag("Resumes")
interface Resumes {
  @doc("Get the signed-in user's resume. Returns 404 when the user has not saved one yet.")
  @get
  getMyResume(): Resume | UnauthorizedProblem | ResumeNotFoundProblem;

  @doc("Create or replace the signed-in user's resume. Blank or oversized text is rejected with 400 and leaves any previous resume unchanged.")
  @put
  saveMyResume(@body request: SaveResumeRequest): Resume | BadRequestProblem | UnauthorizedProblem;
}
